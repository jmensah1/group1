Public Module AdminEditors
    Public Sub Edit(owner As Form, session As LoginSession, kind As String, id As String)
        Dim d = DataStore.Snapshot(session)
        Using e As New EntryDialog(If(id Is Nothing, "Add ", "Edit ") & kind.TrimEnd("s"c).ToLowerInvariant())
            Select Case kind
                Case "Buses"
                    Dim b = If(id Is Nothing, New Bus(), d.Buses.Single(Function(x) x.Id = id))
                    e.AddText("number", "Bus number", b.Number)
                    e.AddNumber("capacity", "Seat capacity", b.Capacity, 1, 100)
                    e.AddText("driver", "Driver name", b.Driver)
                    e.AddNote("A bus capacity cannot be reduced below seats already sold on any of its trips.")
                    e.SubmitAction = Sub() DataStore.SaveBus(session, New Bus With {.Id = b.Id, .Number = e.TextValue("number"), .Capacity = CInt(e.NumberValue("capacity")), .Driver = e.TextValue("driver")})
                Case "Routes"
                    Dim r = If(id Is Nothing, New BusRoute(), d.Routes.Single(Function(x) x.Id = id))
                    Dim stations = d.Stations.Select(Function(s) New Choice(s, s)).ToList()
                    e.AddChoice("from", "Origin station", stations, r.Origin)
                    e.AddChoice("to", "Destination", stations, r.Destination)
                    e.AddNumber("fare", "Fare (GHS)", r.Fare, 0.01D, 10000D, 2)
                    e.AddNumber("distance", "Distance (km)", r.Distance, 1, 2000)
                    e.AddNote("Price changes apply to future sales. Existing tickets retain their original fare.")
                    e.SubmitAction = Sub() DataStore.SaveRoute(session, New BusRoute With {.Id = r.Id, .Origin = e.ChoiceValue("from"), .Destination = e.ChoiceValue("to"), .Fare = e.NumberValue("fare"), .Distance = CInt(e.NumberValue("distance"))})
                Case "Trips"
                    Dim t = If(id Is Nothing, New Trip(), d.Trips.Single(Function(x) x.Id = id))
                    e.AddChoice("route", "Route", d.Routes.Select(Function(r) New Choice(r.Id, r.Origin & " → " & r.Destination)), t.RouteId)
                    e.AddChoice("bus", "Bus", d.Buses.Select(Function(b) New Choice(b.Id, b.Number & " (" & b.Capacity & " seats)")), t.BusId)
                    e.AddDate("departure", "Departure", t.Departure)
                    e.AddChoice("bookman", "Bookman", d.Users.Where(Function(u) u.Active AndAlso u.Role = "Bookman").Select(Function(u) New Choice(u.Id, u.Name & " · " & u.Station)), t.BookmanId)
                    e.AddNote("Assign a bookman from the origin station. Trips with ticket history keep their route, bus and time. Use the trip status buttons to load or depart a bus.")
                    e.SubmitAction = Sub() DataStore.SaveTrip(session, New Trip With {.Id = t.Id, .RouteId = e.ChoiceValue("route"), .BusId = e.ChoiceValue("bus"), .Departure = e.DateValue("departure"), .BookmanId = e.ChoiceValue("bookman")})
                Case "Users"
                    Dim u = If(id Is Nothing, New UserAccount(), d.Users.Single(Function(x) x.Id = id))
                    e.AddText("username", "Username", u.Username)
                    e.AddText("name", "Full name", u.Name)
                    e.AddText("phone", "Ghana phone", u.Phone)
                    e.AddChoice("role", "Role", {New Choice("Passenger", "Passenger"), New Choice("Bookman", "Bookman"), New Choice("Admin", "Admin")}, u.Role)
                    e.AddChoice("station", "Station", d.Stations.Select(Function(s) New Choice(s, s)), u.Station)
                    e.AddCheck("active", "Active account", u.Active)
                    e.AddText("password", If(id Is Nothing, "Password", "New password"), "", True)
                    e.AddNote(If(id Is Nothing, "A new account needs a password of at least 8 characters.", "Leave password blank to keep the current one. Disable accounts to preserve their audit history."))
                    e.SubmitAction = Sub() DataStore.SaveUser(session, New UserAccount With {.Id = u.Id, .Username = e.TextValue("username"), .Name = e.TextValue("name"), .Phone = e.TextValue("phone"), .Role = e.ChoiceValue("role"), .Station = e.ChoiceValue("station"), .Active = e.CheckValue("active")}, e.TextValue("password"))
            End Select
            e.ShowDialog(owner)
        End Using
    End Sub
End Module
