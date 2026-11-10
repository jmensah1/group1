Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports System.Threading

' Every read/modify/write transaction holds an OS file lock shared by all app copies.
' state.json is the atomic commit record; the required JSON files are readable projections.
' A crash during projection writes is recovered from state.json on the next transaction.
Public Module DataStore
    Public Property Root As String = Path.Combine(AppContext.BaseDirectory, "Data")
    Private ReadOnly JsonOptions As New JsonSerializerOptions With {.WriteIndented = True}
    Private ReadOnly Sessions As New Dictionary(Of String, String)
    Private ReadOnly SessionGenerations As New Dictionary(Of String, String)
    Private Const Iterations As Integer = 150000

    Private Function Locked(Of T)(action As Func(Of T)) As T
        Directory.CreateDirectory(Root)
        Dim deadline = DateTime.UtcNow.AddSeconds(15)
        Dim handle As FileStream = Nothing
        Do
            Try
                handle = New FileStream(Path.Combine(Root, ".transaction.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
            Catch ex As IOException
                If DateTime.UtcNow > deadline Then Throw New IOException("Data is busy. Please retry in a moment.", ex)
                Thread.Sleep(35)
            End Try
        Loop While handle Is Nothing
        Using handle
            Return action()
        End Using
    End Function

    Private Sub AtomicWrite(file As String, value As Object)
        Dim temp = file & "." & Guid.NewGuid().ToString("N") & ".tmp"
        Try
            Dim bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, value.GetType(), JsonOptions))
            Using stream As New FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)
                stream.Write(bytes)
                stream.Flush(True)
            End Using
            IO.File.Move(temp, file, True)
        Finally
            If IO.File.Exists(temp) Then IO.File.Delete(temp)
        End Try
    End Sub

    Private Function LoadState() As AppData
        Dim file = Path.Combine(Root, "state.json")
        If Not IO.File.Exists(file) Then Throw New IOException("state.json is missing. Restore your Data backup; the app will not silently discard existing data.")
        Dim data = JsonSerializer.Deserialize(Of AppData)(IO.File.ReadAllText(file), JsonOptions)
        If data Is Nothing OrElse data.SigningKey = "" Then Throw New IOException("The saved data is invalid. Restore a backup.")
        Normalize(data)
        Return data
    End Function

    Private Sub SaveState(data As AppData)
        Normalize(data)
        AtomicWrite(Path.Combine(Root, "state.json"), data)
        WriteProjections(Root, data)
    End Sub

    Private Sub WriteProjections(folder As String, d As AppData)
        Directory.CreateDirectory(folder)
        AtomicWrite(Path.Combine(folder, "users.json"), d.Users)
        AtomicWrite(Path.Combine(folder, "stations.json"), d.Stations)
        AtomicWrite(Path.Combine(folder, "routes.json"), d.Routes)
        AtomicWrite(Path.Combine(folder, "buses.json"), d.Buses)
        AtomicWrite(Path.Combine(folder, "trips.json"), d.Trips)
        AtomicWrite(Path.Combine(folder, "tickets.json"), d.Tickets)
        AtomicWrite(Path.Combine(folder, "payments.json"), d.Payments)
        AtomicWrite(Path.Combine(folder, "smslog.json"), d.SmsLog)
        AtomicWrite(Path.Combine(folder, "auditlog.json"), d.AuditLog)
    End Sub

    Public Sub Initialize()
        Locked(Function()
                   If Not File.Exists(Path.Combine(Root, "state.json")) Then
                       If Directory.GetFiles(Root, "*.json").Length > 0 Then Throw New IOException("Existing JSON files found without a commit record. Restore the complete Data folder.")
                       Dim seed = CreateSeed()
                       SaveState(seed)
                       Dim seedFolder = Path.Combine(Path.GetDirectoryName(Root), "Data_Seed")
                       Directory.CreateDirectory(seedFolder)
                       AtomicWrite(Path.Combine(seedFolder, "state.json"), seed)
                       WriteProjections(seedFolder, seed)
                   Else
                       Dim d = LoadState()
                       SaveState(d)
                   End If
                   Return True
               End Function)
    End Sub

    Public Function Snapshot(session As LoginSession) As AppData
        Return Locked(Function()
                          Dim d = LoadState()
                          Dim u = Actor(d, session)
                          d.SigningKey = ""
                          For Each account In d.Users
                              account.PasswordHash = ""
                              account.Salt = ""
                          Next
                          If u.Role = "Passenger" Then
                              d.Tickets = d.Tickets.Where(Function(t) t.UserId = u.Id).ToList()
                              d.Payments = d.Payments.Where(Function(p) d.Tickets.Any(Function(t) t.Number = p.Ticket)).ToList()
                              d.SmsLog.Clear()
                              d.AuditLog.Clear()
                              d.Users = d.Users.Where(Function(x) x.Id = u.Id).ToList()
                          End If
                          Return d
                      End Function)
    End Function

    Private Sub Normalize(d As AppData)
        For Each t In d.Trips
            Dim capacity = d.Buses.Single(Function(b) b.Id = t.BusId).Capacity
            t.SeatsSold = d.Tickets.Where(Function(x) x.TripId = t.Id AndAlso x.Status <> "Cancelled" AndAlso x.Status <> "Expired").Count()
            If t.SeatsSold > capacity Then Throw New InvalidDataException("Stored tickets exceed bus capacity. Restore a valid backup.")
            If t.Status = "Departed" Then Continue For
            If t.SeatsSold >= capacity Then
                If t.Status <> "Full" Then t.ResumeStatus = t.Status
                t.Status = "Full"
            ElseIf t.Status = "Full" Then
                t.Status = If(t.ResumeStatus = "Loading", "Loading", "Scheduled")
            End If
        Next
    End Sub

    Private Sub Log(d As AppData, who As String, action As String, details As String)
        d.AuditLog.Add(New AuditEntry With {.User = who, .Action = action, .Details = details})
    End Sub

    Private Function Actor(d As AppData, session As LoginSession, ParamArray roles() As String) As UserAccount
        If session Is Nothing OrElse Not Sessions.ContainsKey(session.Token) Then Throw New UnauthorizedAccessException("Sign in again to continue.")
        If Not SessionGenerations.ContainsKey(session.Token) OrElse SessionGenerations(session.Token) <> d.SigningKey Then Throw New UnauthorizedAccessException("Demo data was reset. Sign in again.")
        Dim id = Sessions(session.Token)
        Dim u = d.Users.SingleOrDefault(Function(x) x.Id = id AndAlso x.Active)
        If u Is Nothing Then Throw New UnauthorizedAccessException("This account is disabled. Sign in again.")
        If roles.Length > 0 AndAlso Not roles.Contains(u.Role) Then Throw New UnauthorizedAccessException("Your role is not allowed to perform this action.")
        Return u
    End Function

    Public Sub Logout(session As LoginSession)
        If session IsNot Nothing Then
            Sessions.Remove(session.Token)
            SessionGenerations.Remove(session.Token)
        End If
    End Sub

    Private Sub SetPassword(u As UserAccount, password As String)
        If password.Length < 8 OrElse password.Length > 128 Then Throw New ArgumentException("Use a password of 8 to 128 characters.")
        Dim saltBytes = RandomNumberGenerator.GetBytes(16)
        u.Salt = Convert.ToBase64String(saltBytes)
        u.PasswordHash = Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, 32))
    End Sub

    Public Function Login(username As String, password As String) As LoginSession
        Dim errorText As String = Nothing
        Dim result = Locked(Function()
                                Dim d = LoadState()
                                Dim u = d.Users.SingleOrDefault(Function(x) String.Equals(x.Username, username.Trim(), StringComparison.OrdinalIgnoreCase))
                                If u Is Nothing OrElse Not u.Active Then
                                    errorText = "Incorrect username or password."
                                    Log(d, username.Trim(), "Login rejected", "Unknown or inactive account")
                                ElseIf u.LockedUntil > DateTimeOffset.UtcNow Then
                                    errorText = "Account locked. Try again in " & Math.Ceiling((u.LockedUntil - DateTimeOffset.UtcNow).TotalSeconds).ToString() & " seconds."
                                Else
                                    Dim actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(u.Salt), Iterations, HashAlgorithmName.SHA256, 32)
                                    If Not CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(u.PasswordHash)) Then
                                        u.FailedAttempts += 1
                                        If u.FailedAttempts >= 5 Then
                                            u.LockedUntil = DateTimeOffset.UtcNow.AddMinutes(1)
                                            u.FailedAttempts = 0
                                        End If
                                        errorText = If(u.LockedUntil > DateTimeOffset.UtcNow, "Account locked for one minute after 5 incorrect passwords.", "Incorrect username or password.")
                                        Log(d, u.Username, "Login rejected", "Incorrect password")
                                    Else
                                        u.FailedAttempts = 0
                                        u.LockedUntil = DateTimeOffset.MinValue
                                        Dim s As New LoginSession With {.UserId = u.Id, .Username = u.Username, .Name = u.Name, .Role = u.Role, .Station = u.Station}
                                        Sessions(s.Token) = u.Id
                                        SessionGenerations(s.Token) = d.SigningKey
                                        Log(d, u.Username, "Login", u.Role)
                                        SaveState(d)
                                        Return s
                                    End If
                                End If
                                SaveState(d)
                                Return CType(Nothing, LoginSession)
                            End Function)
        If errorText IsNot Nothing Then Throw New ArgumentException(errorText)
        Return result
    End Function

    Public Sub ValidatePerson(name As String, phone As String)
        If String.IsNullOrWhiteSpace(name) OrElse name.Trim().Length > 100 Then Throw New ArgumentException("Enter a passenger name (1 to 100 characters).")
        If Not Regex.IsMatch(phone.Trim(), "^(0[235][0-9]{8}|\+233[235][0-9]{8})$") Then Throw New ArgumentException("Enter a Ghana phone number such as 0241234567 or +233241234567.")
    End Sub

    Public Sub Register(username As String, password As String, name As String, phone As String)
        ValidatePerson(name, phone)
        Locked(Function()
                   Dim d = LoadState()
                   ValidateUsername(d, username, "")
                   Dim u As New UserAccount With {.Username = username.Trim(), .Name = name.Trim(), .Phone = phone.Trim()}
                   SetPassword(u, password)
                   d.Users.Add(u)
                   Log(d, u.Username, "Registration", "Passenger account created")
                   SaveState(d)
                   Return True
               End Function)
    End Sub

    Private Sub ValidateUsername(d As AppData, username As String, id As String)
        If Not Regex.IsMatch(username.Trim(), "^[A-Za-z0-9_.-]{3,30}$") Then Throw New ArgumentException("Username: 3 to 30 letters, numbers, dots, hyphens or underscores.")
        If d.Users.Any(Function(u) u.Id <> id AndAlso String.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase)) Then Throw New ArgumentException("That username is already in use.")
    End Sub

    Private Sub CheckStationAccess(d As AppData, u As UserAccount, t As Trip)
        If u.Role = "Bookman" Then
            Dim r = d.Routes.Single(Function(x) x.Id = t.RouteId)
            If r.Origin <> u.Station Then Throw New UnauthorizedAccessException("You can only serve trips at " & u.Station & ".")
        End If
    End Sub

    ' One shared sale entry point. Reload, count, authorize and commit under the same cross-process lock.
    Public Function Sell(session As LoginSession, tripId As String, passenger As String, phone As String, method As String, momoApproved As Boolean) As Ticket
        ValidatePerson(passenger, phone)
        Return Locked(Function()
                          Dim d = LoadState()
                          Dim u = Actor(d, session, "Passenger", "Bookman", "Admin")
                          If Not {"Cash", "MoMo", "Pay at station"}.Contains(method) Then Throw New ArgumentException("Choose a valid payment method.")
                          If u.Role = "Passenger" AndAlso method = "Cash" Then Throw New UnauthorizedAccessException("Cash is collected by station staff. Choose Pay at station.")
                          If u.Role <> "Passenger" AndAlso method = "Pay at station" Then Throw New ArgumentException("Counter sales require Cash or MoMo payment.")
                          If method = "MoMo" AndAlso Not momoApproved Then Throw New ArgumentException("Complete the mock MoMo approval first.")
                          Dim t = d.Trips.Single(Function(x) x.Id = tripId)
                          CheckStationAccess(d, u, t)
                          Dim b = d.Buses.Single(Function(x) x.Id = t.BusId)
                          If t.Status = "Departed" Then Throw New ArgumentException("This bus has departed.")
                          If t.SeatsSold >= b.Capacity OrElse t.Status = "Full" Then Throw New ArgumentException("This trip is full. No tickets were issued.")
                          Dim r = d.Routes.Single(Function(x) x.Id = t.RouteId)
                          Dim number As String
                          Do
                              number = "GH-" & Convert.ToHexString(RandomNumberGenerator.GetBytes(6))
                          Loop While d.Tickets.Any(Function(x) x.Number = number)
                          Dim ticket As New Ticket With {.Number = number, .TripId = t.Id, .UserId = If(u.Role = "Passenger", u.Id, ""), .Passenger = passenger.Trim(), .Phone = phone.Trim(), .Amount = r.Fare, .PaymentMethod = method, .Status = If(method = "Pay at station", "Booked", "Paid")}
                          d.Tickets.Add(ticket)
                          d.Payments.Add(New Payment With {.Ticket = number, .Method = method, .Amount = r.Fare, .Status = If(ticket.Status = "Paid", "Successful", "Pending")})
                          d.SmsLog.Add(New SmsEntry With {.Phone = ticket.Phone, .Message = "Group1 Transport: your ticket code is " & number & ". " & r.Origin & " to " & r.Destination & ", " & t.Departure.ToString("dd MMM HH:mm") & "."})
                          Log(d, u.Username, "Ticket sale", number & "; " & method & "; GHS " & r.Fare.ToString("N2"))
                          SaveState(d)
                          Return ticket
                      End Function)
    End Function

    Public Sub Cancel(session As LoginSession, number As String)
        Locked(Function()
                   Dim d = LoadState()
                   Dim u = Actor(d, session)
                   Dim t = d.Tickets.Single(Function(x) x.Number = number)
                   If u.Role = "Passenger" AndAlso t.UserId <> u.Id Then Throw New UnauthorizedAccessException("You can only cancel your own tickets.")
                   CheckStationAccess(d, u, d.Trips.Single(Function(x) x.Id = t.TripId))
                   If Not {"Booked", "Paid"}.Contains(t.Status) Then Throw New ArgumentException("Only unused Booked or Paid tickets can be cancelled.")
                   If d.Trips.Single(Function(x) x.Id = t.TripId).Status = "Departed" Then Throw New ArgumentException("This bus has already departed.")
                   t.Status = "Cancelled"
                   Dim p = d.Payments.Single(Function(x) x.Ticket = number)
                   p.Status = If(p.Status = "Successful", "Refunded (mock)", "Cancelled")
                   Log(d, u.Username, "Cancellation", number & "; " & p.Status)
                   SaveState(d)
                   Return True
               End Function)
    End Sub

    Public Function QrText(session As LoginSession, number As String) As String
        Return Locked(Function()
                          Dim d = LoadState()
                          Dim u = Actor(d, session)
                          Dim t = d.Tickets.Single(Function(x) x.Number = number)
                          If u.Role = "Passenger" AndAlso t.UserId <> u.Id Then Throw New UnauthorizedAccessException("This ticket belongs to another passenger.")
                          CheckStationAccess(d, u, d.Trips.Single(Function(x) x.Id = t.TripId))
                          Dim payload = "G1|" & t.Number & "|" & t.TripId
                          Return payload & "|" & Sign(d, payload)
                      End Function)
    End Function

    Private Function Sign(d As AppData, payload As String) As String
        Return Convert.ToHexString(HMACSHA256.HashData(Convert.FromBase64String(d.SigningKey), Encoding.UTF8.GetBytes(payload)))
    End Function

    Private Function ResolveTicket(d As AppData, input As String) As Ticket
        input = input.Trim()
        If input.StartsWith("G1|", StringComparison.Ordinal) Then
            Dim p = input.Split("|"c)
            If p.Length <> 4 Then Throw New ArgumentException("Invalid QR format.")
            Dim expected = Encoding.UTF8.GetBytes(Sign(d, String.Join("|", p.Take(3))))
            If Not CryptographicOperations.FixedTimeEquals(expected, Encoding.UTF8.GetBytes(p(3))) Then Throw New ArgumentException("Forged or invalid QR signature.")
            Dim match = d.Tickets.SingleOrDefault(Function(x) x.Number = p(1) AndAlso x.TripId = p(2))
            If match Is Nothing Then Throw New ArgumentException("Ticket not found.")
            Return match
        End If
        Dim found = d.Tickets.SingleOrDefault(Function(x) x.Number = input.ToUpperInvariant())
        If found Is Nothing Then Throw New ArgumentException("Ticket not found. Paste the complete ticket number or QR text.")
        Return found
    End Function

    Public Function Board(session As LoginSession, input As String, collectCash As Boolean) As String
        Return Locked(Function()
                          Dim d = LoadState()
                          Dim u = Actor(d, session, "Bookman", "Admin")
                          Dim ticket = ResolveTicket(d, input)
                          Dim t = d.Trips.Single(Function(x) x.Id = ticket.TripId)
                          CheckStationAccess(d, u, t)
                          If ticket.Status = "Boarded" Then Throw New ArgumentException("Already used. This passenger has already boarded.")
                          If t.Status = "Departed" Then Throw New ArgumentException("Cannot board a departed bus.")
                          If ticket.Status = "Booked" Then
                              If Not collectCash Then Throw New ArgumentException("Payment pending. Confirm cash collection before boarding.")
                              Dim payment = d.Payments.Single(Function(x) x.Ticket = ticket.Number)
                              payment.Status = "Successful"
                              payment.Method = "Cash"
                              ticket.PaymentMethod = "Cash"
                              Log(d, u.Username, "Cash collected", ticket.Number & "; GHS " & ticket.Amount.ToString("N2"))
                              ticket.Status = "Paid"
                          End If
                          If ticket.Status <> "Paid" Then Throw New ArgumentException("This ticket is " & ticket.Status & " and cannot be boarded.")
                          ticket.Status = "Boarded"
                          Log(d, u.Username, "Boarding", ticket.Number)
                          SaveState(d)
                          Return ticket.Passenger & " boarded successfully. " & ticket.Number
                      End Function)
    End Function

    Public Sub ChangeStatus(session As LoginSession, tripId As String, status As String)
        Locked(Function()
                   Dim d = LoadState()
                   Dim u = Actor(d, session, "Bookman", "Admin")
                   Dim t = d.Trips.Single(Function(x) x.Id = tripId)
                   CheckStationAccess(d, u, t)
                   Dim valid = (t.Status = "Scheduled" AndAlso status = "Loading") OrElse ({"Loading", "Full"}.Contains(t.Status) AndAlso status = "Departed")
                   If Not valid Then Throw New ArgumentException("Allowed transitions: Scheduled to Loading; Loading or Full to Departed. Full is set automatically at capacity.")
                   t.Status = status
                   If status = "Loading" Then t.ResumeStatus = status
                   If status = "Departed" Then
                       For Each ticket In d.Tickets.Where(Function(x) x.TripId = t.Id AndAlso x.Status = "Booked")
                           ticket.Status = "Expired"
                           d.Payments.Single(Function(x) x.Ticket = ticket.Number).Status = "Expired"
                       Next
                   End If
                   Log(d, u.Username, "Trip status", t.Id & " → " & status)
                   SaveState(d)
                   Return True
               End Function)
    End Sub

    Public Sub SaveBus(session As LoginSession, value As Bus)
        Locked(Function()
                   Dim d = LoadState()
                   Dim u = Actor(d, session, "Admin")
                   If String.IsNullOrWhiteSpace(value.Number) OrElse String.IsNullOrWhiteSpace(value.Driver) Then Throw New ArgumentException("Enter bus number and driver name.")
                   If value.Capacity < 1 OrElse value.Capacity > 100 Then Throw New ArgumentException("Capacity must be from 1 to 100.")
                   If d.Buses.Any(Function(x) x.Id <> value.Id AndAlso x.Number.Equals(value.Number, StringComparison.OrdinalIgnoreCase)) Then Throw New ArgumentException("Bus number already exists.")
                   For Each t In d.Trips.Where(Function(x) x.BusId = value.Id)
                       If t.SeatsSold > value.Capacity Then Throw New ArgumentException("Capacity cannot be lower than tickets already sold.")
                   Next
                   d.Buses.RemoveAll(Function(x) x.Id = value.Id)
                   d.Buses.Add(value)
                   Log(d, u.Username, "Bus saved", value.Number & "; capacity " & value.Capacity)
                   SaveState(d)
                   Return True
               End Function)
    End Sub

    Public Sub SaveRoute(session As LoginSession, value As BusRoute)
        Locked(Function()
                   Dim d = LoadState()
                   Dim u = Actor(d, session, "Admin")
                   If Not d.Stations.Contains(value.Origin) OrElse Not d.Stations.Contains(value.Destination) OrElse value.Origin = value.Destination Then Throw New ArgumentException("Choose two different stations.")
                   If value.Fare <= 0 OrElse value.Fare > 10000 OrElse value.Distance < 1 OrElse value.Distance > 2000 Then Throw New ArgumentException("Fare: 0.01 to 10,000 GHS; distance: 1 to 2,000 km.")
                   If d.Routes.Any(Function(x) x.Id <> value.Id AndAlso x.Origin = value.Origin AndAlso x.Destination = value.Destination) Then Throw New ArgumentException("This route already exists.")
                   Dim old = d.Routes.SingleOrDefault(Function(x) x.Id = value.Id)
                   Log(d, u.Username, "Route / price change", value.Origin & " → " & value.Destination & "; " & If(old Is Nothing, "new", old.Fare.ToString("N2")) & " → GHS " & value.Fare.ToString("N2"))
                   d.Routes.RemoveAll(Function(x) x.Id = value.Id)
                   d.Routes.Add(value)
                   SaveState(d)
                   Return True
               End Function)
    End Sub

    Public Sub SaveTrip(session As LoginSession, value As Trip)
        Locked(Function()
                   Dim d = LoadState()
                   Dim u = Actor(d, session, "Admin")
                   Dim r = d.Routes.Single(Function(x) x.Id = value.RouteId)
                   If Not d.Buses.Any(Function(x) x.Id = value.BusId) Then Throw New ArgumentException("Choose a bus.")
                   Dim worker = d.Users.SingleOrDefault(Function(x) x.Id = value.BookmanId AndAlso x.Role = "Bookman" AndAlso x.Active)
                   If worker Is Nothing OrElse worker.Station <> r.Origin Then Throw New ArgumentException("Assign an active bookman from the origin station.")
                   Dim old = d.Trips.SingleOrDefault(Function(x) x.Id = value.Id)
                   If old IsNot Nothing AndAlso (old.SeatsSold > 0 OrElse old.Status = "Departed") Then
                       If old.RouteId <> value.RouteId OrElse old.BusId <> value.BusId OrElse old.Departure <> value.Departure Then Throw New ArgumentException("A trip with tickets or a departure record cannot change route, bus or time. Create a new trip.")
                   End If
                   If old Is Nothing OrElse old.SeatsSold = 0 Then
                       If value.Departure < DateTime.Now Then Throw New ArgumentException("Choose a future departure time.")
                   End If
                   If d.Trips.Any(Function(x) x.Id <> value.Id AndAlso x.BusId = value.BusId AndAlso x.Status <> "Departed" AndAlso Math.Abs((x.Departure - value.Departure).TotalHours) < 3) Then Throw New ArgumentException("This bus already has a trip within three hours of that time.")
                   value.Status = If(old Is Nothing, "Scheduled", old.Status)
                   value.ResumeStatus = If(old Is Nothing, "Scheduled", old.ResumeStatus)
                   d.Trips.RemoveAll(Function(x) x.Id = value.Id)
                   d.Trips.Add(value)
                   Log(d, u.Username, "Trip saved", value.Id & "; " & r.Origin & " → " & r.Destination)
                   SaveState(d)
                   Return True
               End Function)
    End Sub

    Public Sub SaveUser(session As LoginSession, value As UserAccount, password As String)
        Locked(Function()
                   Dim d = LoadState()
                   Dim u = Actor(d, session, "Admin")
                   ValidatePerson(value.Name, value.Phone)
                   ValidateUsername(d, value.Username, value.Id)
                   If Not {"Admin", "Bookman", "Passenger"}.Contains(value.Role) OrElse Not d.Stations.Contains(value.Station) Then Throw New ArgumentException("Choose a valid role and station.")
                   Dim old = d.Users.SingleOrDefault(Function(x) x.Id = value.Id)
                   If old IsNot Nothing Then
                       If old.Id = u.Id AndAlso (value.Role <> "Admin" OrElse Not value.Active) Then Throw New ArgumentException("You cannot disable or demote your own admin account.")
                       If d.Trips.Any(Function(x) x.BookmanId = old.Id AndAlso x.Status <> "Departed") AndAlso (value.Role <> "Bookman" OrElse Not value.Active OrElse value.Station <> old.Station) Then Throw New ArgumentException("Reassign this bookman's active trips first.")
                       value.PasswordHash = old.PasswordHash
                       value.Salt = old.Salt
                       value.FailedAttempts = old.FailedAttempts
                       value.LockedUntil = old.LockedUntil
                   End If
                   If old Is Nothing OrElse password <> "" Then SetPassword(value, password)
                   d.Users.RemoveAll(Function(x) x.Id = value.Id)
                   d.Users.Add(value)
                   Log(d, u.Username, "User saved", value.Username & "; " & value.Role & "; active=" & value.Active)
                   SaveState(d)
                   Return True
               End Function)
    End Sub

    Public Sub DeleteEntity(session As LoginSession, kind As String, id As String)
        Locked(Function()
                   Dim d = LoadState()
                   Dim u = Actor(d, session, "Admin")
                   Select Case kind
                       Case "Buses"
                           If d.Trips.Any(Function(x) x.BusId = id) Then Throw New ArgumentException("This bus is used by a trip and cannot be deleted.")
                           d.Buses.RemoveAll(Function(x) x.Id = id)
                       Case "Routes"
                           If d.Trips.Any(Function(x) x.RouteId = id) Then Throw New ArgumentException("This route is used by a trip and cannot be deleted.")
                           d.Routes.RemoveAll(Function(x) x.Id = id)
                       Case "Trips"
                           If d.Tickets.Any(Function(x) x.TripId = id) Then Throw New ArgumentException("Trips with ticket history cannot be deleted.")
                           d.Trips.RemoveAll(Function(x) x.Id = id)
                       Case "Users"
                           Throw New ArgumentException("Preserve user history: edit the account and uncheck Active instead.")
                       Case Else
                           Throw New ArgumentException("Unknown entity.")
                   End Select
                   Log(d, u.Username, "Deleted " & kind, id)
                   SaveState(d)
                   Return True
               End Function)
    End Sub

    Public Sub ResetDemo(session As LoginSession)
        Locked(Function()
                   Dim d = LoadState()
                   Dim u = Actor(d, session, "Admin")
                   Dim backupDir = Path.Combine(Path.GetDirectoryName(Root), "Backups")
                   Directory.CreateDirectory(backupDir)
                   AtomicWrite(Path.Combine(backupDir, "before-reset-" & DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") & ".json"), d)
                   Dim seed = CreateSeed()
                   Log(seed, u.Username, "Demo reset", "Previous data preserved in Backups; all sessions invalidated")
                   SaveState(seed)
                   Sessions.Clear()
                   SessionGenerations.Clear()
                   Return True
               End Function)
    End Sub

    Private Function CreateSeed() As AppData
        Dim d As New AppData With {.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}
        d.Stations.AddRange({"Accra", "Kumasi", "Tamale", "Takoradi", "Cape Coast"})
        Dim admin As New UserAccount With {.Id = "admin", .Username = "admin", .Role = "Admin", .Name = "Demo Administrator", .Phone = "0241234567"}
        Dim worker As New UserAccount With {.Id = "bookman", .Username = "bookman", .Role = "Bookman", .Name = "Kwame Mensah", .Phone = "0242345678"}
        Dim passenger As New UserAccount With {.Id = "passenger", .Username = "passenger", .Name = "Ama Owusu", .Phone = "0243456789"}
        For Each u In {admin, worker, passenger}
            SetPassword(u, "Demo@123")
            d.Users.Add(u)
        Next
        d.Routes.AddRange({New BusRoute With {.Id = "r1", .Origin = "Accra", .Destination = "Kumasi", .Fare = 150D, .Distance = 250}, New BusRoute With {.Id = "r2", .Origin = "Accra", .Destination = "Cape Coast", .Fare = 90D, .Distance = 150}, New BusRoute With {.Id = "r3", .Origin = "Accra", .Destination = "Tamale", .Fare = 280D, .Distance = 610}, New BusRoute With {.Id = "r4", .Origin = "Accra", .Destination = "Takoradi", .Fare = 120D, .Distance = 220}})
        d.Buses.AddRange({New Bus With {.Id = "b1", .Number = "GT 2045-26", .Capacity = 5, .Driver = "Kofi Asare"}, New Bus With {.Id = "b2", .Number = "GT 3890-26", .Capacity = 45, .Driver = "Yaw Boateng"}, New Bus With {.Id = "b3", .Number = "GT 5172-26", .Capacity = 45, .Driver = "Abdul Rahman"}, New Bus With {.Id = "b4", .Number = "GT 6211-26", .Capacity = 38, .Driver = "Esi Addo"}})
        For i As Integer = 1 To 4
            d.Trips.Add(New Trip With {.Id = "t" & i, .RouteId = "r" & i, .BusId = "b" & i, .Departure = DateTime.Today.AddDays(1).AddHours(7 + i), .BookmanId = worker.Id, .Status = If(i = 1, "Loading", "Scheduled"), .ResumeStatus = If(i = 1, "Loading", "Scheduled")})
        Next
        Log(d, "System", "Seed created", "Classroom demo; first bus has 5 seats so Full is easy to demonstrate.")
        Return d
    End Function
End Module
