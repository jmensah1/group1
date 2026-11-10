Imports System.Diagnostics
Imports System.IO
Imports System.Text.Json
Imports QRCoder

' Executed only with --self-test <isolated-folder>; never touches the demo's real Data.
Public Module SelfTests
    Private ReadOnly results As New List(Of String)
    Private Sub Check(condition As Boolean, message As String)
        If Not condition Then Throw New Exception("FAIL: " & message)
        results.Add("PASS: " & message)
    End Sub
    Private Sub Reject(action As Action, expected As String)
        Try
            action()
        Catch ex As Exception
            Check(ex.Message.Contains(expected, StringComparison.OrdinalIgnoreCase), "Rejected action: " & expected)
            Return
        End Try
        Throw New Exception("Expected rejection: " & expected)
    End Sub

    Public Sub Run(folder As String)
        Directory.CreateDirectory(folder)
        DataStore.Root = Path.Combine(folder, "Data")
        Try
            If File.Exists(Path.Combine(DataStore.Root, "state.json")) Then Throw New ArgumentException("Use a fresh empty folder for self-tests; existing data is protected.")
            DataStore.Initialize()
            Dim admin = DataStore.Login("admin", "Demo@123")
            Dim worker = DataStore.Login("bookman", "Demo@123")
            Dim passenger = DataStore.Login("passenger", "Demo@123")
            Check(DataStore.Snapshot(admin).Trips.Count = 4, "Seed contains four routes, buses and future trips")
            Dim raw = JsonSerializer.Deserialize(Of AppData)(File.ReadAllText(Path.Combine(DataStore.Root, "state.json")))
            Check(raw.Users.All(Function(u) u.PasswordHash <> "Demo@123" AndAlso u.Salt.Length > 0), "Passwords are salted PBKDF2 hashes")
            Check(raw.Users.Select(Function(u) u.PasswordHash).Distinct().Count() = 3, "Identical passwords have distinct salted hashes")
            Check(DataStore.Snapshot(passenger).SigningKey = "" AndAlso DataStore.Snapshot(passenger).Users.All(Function(u) u.PasswordHash = ""), "UI snapshots do not expose signing key or password hashes")
            Reject(Sub() DataStore.Login("admin", "wrong"), "Incorrect")
            DataStore.Register("locktest", "Demo@123", "Test Passenger", "0241112222")
            For i As Integer = 1 To 4
                Reject(Sub() DataStore.Login("locktest", "wrong"), "Incorrect")
            Next
            Reject(Sub() DataStore.Login("locktest", "wrong"), "locked")
            Reject(Sub() DataStore.Login("locktest", "Demo@123"), "locked")
            Reject(Sub() DataStore.Register("passenger", "Demo@123", "Duplicate", "0241112222"), "already")
            Reject(Sub() DataStore.Register("badphone", "Demo@123", "Test", "123"), "Ghana")
            Reject(Sub() DataStore.Register("shortpass", "short", "Test", "0241112222"), "8 to 128")
            passenger.Role = "Admin"
            Reject(Sub() DataStore.SaveBus(passenger, New Bus With {.Number = "EVIL", .Driver = "Test"}), "role")
            passenger.Role = "Passenger"
            Reject(Sub() DataStore.Sell(passenger, "t1", "Test", "0241112222", "Cash", False), "Cash")
            Reject(Sub() DataStore.Sell(passenger, "t1", "Test", "0241112222", "MoMo", False), "approval")
            Dim paid = DataStore.Sell(passenger, "t1", "Ama", "0241112222", "MoMo", True)
            Check(paid.Status = "Paid" AndAlso paid.Number.StartsWith("GH-"), "Approved mock MoMo creates a Paid cryptographic ticket")
            Dim qr = DataStore.QrText(passenger, paid.Number)
            Using generator As New QRCodeGenerator(), code = generator.CreateQrCode(qr, QRCodeGenerator.ECCLevel.Q), renderer As New PngByteQRCode(code)
                Dim bytes = renderer.GetGraphic(4)
                Check(bytes.Length > 100 AndAlso bytes(0) = 137 AndAlso bytes(1) = 80, "QRCoder produces a real PNG QR image")
            End Using
            Reject(Sub() DataStore.Board(worker, qr.Substring(0, qr.Length - 1) & If(qr.EndsWith("0"), "1", "0"), False), "signature")
            Check(DataStore.Board(worker, qr, False).Contains("successfully"), "Valid signed QR boards a paid ticket")
            Reject(Sub() DataStore.Board(worker, qr, False), "Already used")
            Reject(Sub() DataStore.Cancel(passenger, paid.Number), "unused")
            Dim booked = DataStore.Sell(passenger, "t1", "Ama", "0241112222", "Pay at station", False)
            Reject(Sub() DataStore.Board(worker, booked.Number, False), "Payment pending")
            Check(DataStore.Board(worker, booked.Number, True).Contains("successfully"), "Pay at station cash is collected before boarding")
            Dim cancelled = DataStore.Sell(passenger, "t1", "Ama", "0241112222", "MoMo", True)
            DataStore.Cancel(passenger, cancelled.Number)
            Check(DataStore.Snapshot(admin).Trips.Single(Function(t) t.Id = "t1").SeatsSold = 2, "Cancellation releases the seat")
            Check(DataStore.Snapshot(admin).Payments.Single(Function(p) p.Ticket = cancelled.Number).Status = "Refunded (mock)", "Cancellation records a mock refund")
            Dim route = DataStore.Snapshot(admin).Routes.Single(Function(r) r.Id = "r1")
            route.Fare = 175D
            DataStore.SaveRoute(admin, route)
            Check(DataStore.Snapshot(admin).Tickets.Single(Function(t) t.Number = paid.Number).Amount = 150D, "Route price changes preserve historical ticket prices")
            Dim foreign As New UserAccount With {.Username = "kumasi", .Name = "Kumasi Staff", .Phone = "0242223333", .Role = "Bookman", .Station = "Kumasi"}
            DataStore.SaveUser(admin, foreign, "Demo@123")
            Dim otherWorker = DataStore.Login("kumasi", "Demo@123")
            Reject(Sub() DataStore.Sell(otherWorker, "t1", "Test", "0241112222", "Cash", False), "only serve")
            Dim counter = DataStore.Sell(worker, "t1", "Counter Customer", "0241112222", "Cash", False)
            Check(DataStore.Snapshot(passenger).Tickets.All(Function(t) t.UserId = passenger.UserId), "Passenger ticket snapshots contain only their own tickets")
            Reject(Sub() DataStore.Cancel(passenger, counter.Number), "own tickets")
            Dim smallBus = DataStore.Snapshot(admin).Buses.Single(Function(b) b.Id = "b1")
            smallBus.Capacity = 1
            Reject(Sub() DataStore.SaveBus(admin, smallBus), "lower")
            Reject(Sub() DataStore.DeleteEntity(admin, "Buses", "b1"), "used")
            Dim registered = DataStore.Login("passenger", "Demo@123")
            Dim disabled = DataStore.Snapshot(admin).Users.Single(Function(u) u.Id = "passenger")
            disabled.Active = False
            DataStore.SaveUser(admin, disabled, "")
            Reject(Sub() DataStore.Sell(registered, "t2", "Test", "0241112222", "MoMo", True), "disabled")
            disabled.Active = True
            DataStore.SaveUser(admin, disabled, "")
            ' Repair a deliberately interrupted secondary-file write from the authoritative commit.
            File.WriteAllText(Path.Combine(DataStore.Root, "tickets.json"), "interrupted projection")
            DataStore.Initialize()
            Check(JsonSerializer.Deserialize(Of List(Of Ticket))(File.ReadAllText(Path.Combine(DataStore.Root, "tickets.json"))).Count = DataStore.Snapshot(admin).Tickets.Count, "Projection crash recovery preserves all committed tickets")
            DataStore.ResetDemo(admin)
            Reject(Sub() DataStore.Snapshot(passenger), "Sign in")
            admin = DataStore.Login("admin", "Demo@123")
            worker = DataStore.Login("bookman", "Demo@123")
            passenger = DataStore.Login("passenger", "Demo@123")
            Check(Directory.GetFiles(Path.Combine(folder, "Backups")).Length > 0, "Demo reset preserves a backup")
            Dim workers As New List(Of Process)
            For i As Integer = 1 To 3
                Dim info As New ProcessStartInfo(Environment.ProcessPath) With {.UseShellExecute = False, .CreateNoWindow = True, .WindowStyle = ProcessWindowStyle.Hidden}
                info.ArgumentList.Add("--race-worker")
                info.ArgumentList.Add(folder)
                workers.Add(Process.Start(info))
            Next
            For Each p In workers
                If Not p.WaitForExit(30000) Then
                    p.Kill()
                    Throw New Exception("Race worker timed out")
                End If
                Check(p.ExitCode = 0, "Concurrent seller process completed successfully")
                p.Dispose()
            Next
            Dim race = DataStore.Snapshot(admin)
            Check(race.Tickets.Count = 5 AndAlso race.Trips.Single(Function(t) t.Id = "t1").Status = "Full", "Three processes attempting 60 sales issue exactly 5 tickets for 5 seats")
            Check(race.Tickets.Select(Function(t) t.Number).Distinct().Count() = 5, "Concurrent ticket numbers are unique")
            Check(race.Payments.Count = 5 AndAlso race.SmsLog.Count = 5, "No orphan payments or SMS records under concurrent sales")
            Reject(Sub() DataStore.Sell(passenger, "t1", "Test", "0241112222", "MoMo", True), "full")
            DataStore.Cancel(admin, race.Tickets(0).Number)
            Check(DataStore.Snapshot(admin).Trips.Single(Function(t) t.Id = "t1").Status = "Loading", "Cancelling a full bus reopens Loading and one seat")
            DataStore.Sell(worker, "t1", "Replacement", "0241112222", "Cash", False)
            Check(DataStore.Snapshot(admin).Trips.Single(Function(t) t.Id = "t1").Status = "Full", "Replacement sale fills the bus again")
            DataStore.ChangeStatus(worker, "t1", "Departed")
            Reject(Sub() DataStore.Sell(worker, "t1", "Late", "0241112222", "Cash", False), "departed")
            Reject(Sub() DataStore.Board(worker, race.Tickets(1).Number, False), "departed")
            Dim unpaid = DataStore.Sell(passenger, "t2", "Unpaid", "0241112222", "Pay at station", False)
            DataStore.ChangeStatus(worker, "t2", "Loading")
            DataStore.ChangeStatus(worker, "t2", "Departed")
            Check(DataStore.Snapshot(admin).Tickets.Single(Function(t) t.Number = unpaid.Number).Status = "Expired", "Unpaid bookings expire when the bus departs")
            Dim audit = DataStore.Snapshot(admin).AuditLog
            Check({"Ticket sale", "Boarding", "Cancellation", "Trip status", "Demo reset"}.Where(Function(a) a <> "Boarding").All(Function(a) audit.Any(Function(x) x.Action = a)), "Core operational actions are audited")
            results.Add("ALL " & results.Count & " CHECKS PASSED")
            File.WriteAllLines(Path.Combine(folder, "validation.txt"), results)
            Environment.ExitCode = 0
        Catch ex As Exception
            results.Add(ex.ToString())
            File.WriteAllLines(Path.Combine(folder, "validation.txt"), results)
            Environment.ExitCode = 1
        End Try
    End Sub

    Public Sub RaceWorker(folder As String)
        DataStore.Root = Path.Combine(folder, "Data")
        Try
            Dim session = DataStore.Login("bookman", "Demo@123")
            For i As Integer = 1 To 20
                Try
                    DataStore.Sell(session, "t1", "Race Passenger", "0241112222", "Cash", False)
                Catch ex As ArgumentException When ex.Message.Contains("full", StringComparison.OrdinalIgnoreCase)
                    ' Expected once another process has claimed the remaining capacity.
                End Try
            Next
            Environment.ExitCode = 0
        Catch ex As Exception
            File.WriteAllText(Path.Combine(folder, "race-error-" & Environment.ProcessId & ".txt"), ex.ToString())
            Environment.ExitCode = 1
        End Try
    End Sub
End Module
