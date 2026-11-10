Imports System.Data
Imports System.Globalization

Public Class WorkspaceForm
    Inherits Form
    Private ReadOnly session As LoginSession
    Private ReadOnly tabs As New TabControl With {.Dock = DockStyle.Fill, .Padding = New Point(16, 10)}
    Private ReadOnly clockTimer As New Timer With {.Interval = 4000}
    Private ReadOnly syncLabel As Label = Ui.LabelText("", 9, False, Color.White)
    Private ReadOnly cards As FlowLayoutPanel = Ui.Flow(True)
    Private ReadOnly fromBox As New ComboBox With {.Font = New Font("Segoe UI", 14), .DropDownStyle = ComboBoxStyle.DropDownList, .Width = 180}
    Private ReadOnly toBox As New ComboBox With {.Font = New Font("Segoe UI", 14), .DropDownStyle = ComboBoxStyle.DropDownList, .Width = 190}
    Private ReadOnly destinationFilter As New TextBox With {.Font = New Font("Segoe UI", 16), .Width = 260, .PlaceholderText = "Filter destination…"}
    Private ReadOnly grids As New Dictionary(Of String, DataGridView)
    Private ReadOnly overview As New Panel With {.Dock = DockStyle.Fill, .AutoScroll = True, .Padding = New Padding(16)}
    Private ReadOnly scanBox As New TextBox With {.Font = New Font("Segoe UI", 18), .Width = 540, .PlaceholderText = "Ticket number or signed QR text", .MaxLength = 300}
    Private ReadOnly cashCheck As New CheckBox With {.Text = "I collected cash for a Pay at station ticket", .AutoSize = True, .Margin = New Padding(8, 14, 8, 8)}
    Private busy As Boolean
    Private lastCards As String = ""
    Private lastOverview As String = ""

    Public Sub New(current As LoginSession)
        session = current
        Ui.Style(Me, If(session.Role = "Passenger", "Your journeys", If(session.Role = "Bookman", "Station desk", "Operations centre")))
        Dim top As New TableLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 3, .BackColor = Ui.Ink, .Padding = New Padding(20, 10, 20, 10)}
        top.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        top.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        top.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        Dim branding As New FlowLayoutPanel With {.AutoSize = True, .Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .WrapContents = False}
        branding.Controls.Add(Ui.LabelText("GROUP1 TRANSPORT", 22, True, Color.White))
        branding.Controls.Add(Ui.LabelText(session.Name & "  ·  " & session.Role & If(session.Role = "Bookman", "  ·  " & session.Station, ""), 14, False, Ui.Gold))
        branding.Controls.Add(syncLabel)
        top.Controls.Add(New PictureBox With {.Image = Ui.BrandLogo(), .BackColor = Color.White, .SizeMode = PictureBoxSizeMode.Zoom, .Size = New Size(76, 76), .Tag = "scale", .Margin = New Padding(0, 12, 20, 0)}, 0, 0)
        top.Controls.Add(branding, 1, 0)
        Dim headerActions As New FlowLayoutPanel With {.AutoSize = True, .FlowDirection = FlowDirection.TopDown, .WrapContents = False}
        Dim projector As Button = Nothing
        projector = Ui.ButtonText(If(Ui.PresentationMode, "Presentation: On", "Presentation: Off"), Sub()
                                                                                                     Ui.TogglePresentation()
                                                                                                     projector.Text = If(Ui.PresentationMode, "Presentation: On", "Presentation: Off")
                                                                                                 End Sub, Ui.Gold)
        projector.Font = New Font("Segoe UI", 12, FontStyle.Bold)
        headerActions.Controls.Add(projector)
        headerActions.Controls.Add(Ui.ButtonText("Sign out", Sub() Close(), Ui.Muted))
        top.Controls.Add(headerActions, 2, 0)
        Controls.Add(tabs)
        Controls.Add(top)
        If session.Role = "Admin" Then
            AddPage("Overview").Controls.Add(overview)
            AddGridPage("Live operations")
            AddManagement("Routes")
            AddManagement("Buses")
            AddManagement("Trips")
            AddManagement("Users")
            AddGridPage("Audit log")
        Else
            BuildTripsPage()
            If session.Role = "Bookman" Then BuildValidationPage()
            BuildTicketsPage()
        End If
        AddHandler tabs.SelectedIndexChanged, Sub() RefreshData(True)
        AddHandler clockTimer.Tick, Sub() RefreshData(False)
        AddHandler Shown, Sub()
                              RefreshData(True)
                              clockTimer.Start()
                          End Sub
        AddHandler FormClosed, Sub()
                                   clockTimer.Stop()
                                   clockTimer.Dispose()
                               End Sub
    End Sub

    Private Function AddPage(title As String) As TabPage
        Dim page As New TabPage(title) With {.BackColor = Ui.Paper, .Padding = New Padding(8)}
        tabs.TabPages.Add(page)
        Return page
    End Function

    Private Function AddGridPage(title As String) As TabPage
        Dim page = AddPage(title)
        Dim g = Ui.Grid()
        grids(title) = g
        page.Controls.Add(g)
        Return page
    End Function

    Private Sub BuildTripsPage()
        Dim page = AddPage(If(session.Role = "Passenger", "Find a trip", "Station trips"))
        page.Controls.Add(cards)
        Dim toolbar = Ui.Flow()
        toolbar.Controls.Add(Ui.LabelText("FROM", 10, True))
        toolbar.Controls.Add(fromBox)
        toolbar.Controls.Add(Ui.LabelText("TO", 10, True))
        toolbar.Controls.Add(toBox)
        toolbar.Controls.Add(Ui.ButtonText("Search / refresh", Sub() RefreshData(True)))
        Dim d = DataStore.Snapshot(session)
        fromBox.Items.Add("All stations")
        toBox.Items.Add("All destinations")
        For Each station In d.Stations
            fromBox.Items.Add(station)
            toBox.Items.Add(station)
        Next
        fromBox.SelectedItem = If(session.Role = "Bookman", session.Station, "All stations")
        fromBox.Enabled = session.Role <> "Bookman"
        toBox.SelectedIndex = 0
        page.Controls.Add(toolbar)
        AddHandler fromBox.SelectedIndexChanged, Sub() RefreshData(True)
        AddHandler toBox.SelectedIndexChanged, Sub() RefreshData(True)
    End Sub

    Private Sub BuildTicketsPage()
        Dim title = If(session.Role = "Passenger", "My tickets", "Station tickets")
        Dim page = AddGridPage(title)
        Dim toolbar = Ui.Flow()
        toolbar.Controls.Add(Ui.ButtonText("View / print ticket", Sub() ShowExistingTicket(Ui.SelectedId(grids(title)))))
        toolbar.Controls.Add(Ui.ButtonText("Cancel selected ticket", Sub()
                                                                        Dim number = Ui.SelectedId(grids(title))
                                                                        If MessageBox.Show("Cancel " & number & " and release its seat? Any paid amount is refunded in the mock ledger.", "Cancel ticket", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                                                                            DataStore.Cancel(session, number)
                                                                            RefreshData(True)
                                                                        End If
                                                                    End Sub, Ui.Red))
        toolbar.Controls.Add(Ui.ButtonText("Refresh", Sub() RefreshData(True), Ui.Muted))
        page.Controls.Add(toolbar)
    End Sub

    Private Sub BuildValidationPage()
        Dim page = AddPage("Validate boarding")
        Dim body As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .WrapContents = False, .AutoScroll = True, .Padding = New Padding(32)}
        body.Controls.Add(Ui.LabelText("One ticket. One boarding.", 25, True))
        body.Controls.Add(Ui.LabelText("Paste a ticket number or signed QR text. Only paid, unused tickets can board.", 11))
        body.Controls.Add(scanBox)
        body.Controls.Add(cashCheck)
        body.Controls.Add(Ui.ButtonText("Validate & board passenger", Sub()
                                                                           Dim message = DataStore.Board(session, scanBox.Text, cashCheck.Checked)
                                                                           MessageBox.Show(message, "Boarding confirmed", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                                           scanBox.Clear()
                                                                           cashCheck.Checked = False
                                                                           RefreshData(True)
                                                                       End Sub, Ui.Green))
        body.Controls.Add(Ui.LabelText("A repeated ticket shows Already used. A modified QR signature is rejected." & vbCrLf & "Cash confirmation only applies to unpaid Pay at station bookings.", 10, False, Ui.Muted))
        page.Controls.Add(body)
    End Sub

    Private Sub AddManagement(kind As String)
        Dim page = AddGridPage(kind)
        Dim toolbar = Ui.Flow()
        toolbar.Controls.Add(Ui.ButtonText("Add " & kind.TrimEnd("s"c).ToLowerInvariant(), Sub() EditEntity(kind, Nothing)))
        toolbar.Controls.Add(Ui.ButtonText("Edit selected", Sub() EditEntity(kind, Ui.SelectedId(grids(kind)))))
        toolbar.Controls.Add(Ui.ButtonText(If(kind = "Users", "Disable in editor", "Delete selected"), Sub()
                                                                                                           If kind = "Users" Then
                                                                                                               EditEntity(kind, Ui.SelectedId(grids(kind)))
                                                                                                               Return
                                                                                                           End If
                                                                                                           Dim id = Ui.SelectedId(grids(kind))
                                                                                                           If MessageBox.Show("Delete this unused record? Linked records are protected.", "Delete " & kind, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                                                                                                               DataStore.DeleteEntity(session, kind, id)
                                                                                                               RefreshData(True)
                                                                                                           End If
                                                                                                       End Sub, Ui.Red))
        If kind = "Routes" Then
            toolbar.Controls.Add(Ui.LabelText("Destination", 10, True))
            toolbar.Controls.Add(destinationFilter)
            AddHandler destinationFilter.TextChanged, Sub() RefreshData(True)
        End If
        If kind = "Trips" Then
            toolbar.Controls.Add(Ui.ButtonText("Set Loading", Sub() SetSelectedStatus("Loading"), Ui.Blue))
            toolbar.Controls.Add(Ui.ButtonText("Depart bus", Sub() SetSelectedStatus("Departed"), Ui.Muted))
        End If
        page.Controls.Add(toolbar)
    End Sub

    Private Sub SetSelectedStatus(status As String)
        If status = "Departed" AndAlso MessageBox.Show("Mark this bus Departed? Further sales and boarding will close.", "Depart bus", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
        DataStore.ChangeStatus(session, Ui.SelectedId(grids("Trips")), status)
        RefreshData(True)
    End Sub

    Private Sub RefreshData(force As Boolean)
        If busy OrElse tabs.SelectedTab Is Nothing OrElse IsDisposed Then Return
        Try
            Dim d = DataStore.Snapshot(session)
            Dim title = tabs.SelectedTab.Text
            Select Case title
                Case "Find a trip", "Station trips" : RenderCards(d, force)
                Case "My tickets", "Station tickets" : RenderTickets(d, title)
                Case "Overview" : RenderOverview(d, force)
                Case "Live operations" : RenderOperations(d, title)
                Case "Routes"
                    Ui.Bind(grids(title), Ui.MakeTable({"Id", "From", "Destination", "Fare (GHS):money", "Distance (km):number"}, d.Routes.Where(Function(r) r.Destination.Contains(destinationFilter.Text, StringComparison.OrdinalIgnoreCase)).Select(Function(r) New Object() {r.Id, r.Origin, r.Destination, r.Fare, r.Distance})))
                Case "Buses"
                    Ui.Bind(grids(title), Ui.MakeTable({"Id", "Bus number", "Capacity:number", "Driver"}, d.Buses.Select(Function(b) New Object() {b.Id, b.Number, b.Capacity, b.Driver})))
                Case "Trips" : RenderOperations(d, title)
                Case "Users"
                    Ui.Bind(grids(title), Ui.MakeTable({"Id", "Username", "Name", "Role", "Station", "Phone", "Active"}, d.Users.Select(Function(u) New Object() {u.Id, u.Username, u.Name, u.Role, u.Station, u.Phone, If(u.Active, "Yes", "No")})))
                Case "Audit log"
                    Ui.Bind(grids(title), Ui.MakeTable({"Time:date", "User", "Action", "Details"}, d.AuditLog.OrderByDescending(Function(a) a.Time).Select(Function(a) New Object() {a.Time.LocalDateTime, a.User, a.Action, a.Details})))
            End Select
            syncLabel.Text = "Live · refreshed " & DateTime.Now.ToString("HH:mm:ss") & "  /  every 4 seconds  /  mock payments & SMS"
            ResponsiveUi.Apply(Me)
        Catch ex As UnauthorizedAccessException
            clockTimer.Stop()
            MessageBox.Show(ex.Message, "Sign in required")
            Close()
        Catch ex As Exception
            syncLabel.Text = "Refresh paused: " & ex.Message
            If force Then MessageBox.Show(ex.Message, "Refresh unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub RenderCards(d As AppData, force As Boolean)
        Dim trips = d.Trips.Where(Function(t)
                                      Dim r = d.Routes.Single(Function(x) x.Id = t.RouteId)
                                      Return (session.Role <> "Bookman" OrElse r.Origin = session.Station) AndAlso (fromBox.SelectedIndex <= 0 OrElse r.Origin = fromBox.Text) AndAlso (toBox.SelectedIndex <= 0 OrElse r.Destination = toBox.Text)
                                  End Function).OrderBy(Function(t) If(t.Status = "Loading", 0, If(t.Status = "Departed", 2, 1))).ThenBy(Function(t) t.Departure).ToList()
        Dim signature = String.Join(";", trips.Select(Function(t) t.Id & t.Status & t.SeatsSold & t.Departure.ToString("O") & d.Routes.Single(Function(r) r.Id = t.RouteId).Fare.ToString(CultureInfo.InvariantCulture)))
        If Not force AndAlso lastCards = signature Then Return
        lastCards = signature
        cards.SuspendLayout()
        For Each c As Control In cards.Controls.Cast(Of Control)().ToArray()
            c.Dispose()
        Next
        cards.Controls.Clear()
        If trips.Count = 0 Then cards.Controls.Add(Ui.LabelText("No trips match these stations. Try another destination.", 15, True))
        For i As Integer = 0 To trips.Count - 1
            Dim t = trips(i)
            Dim r = d.Routes.Single(Function(x) x.Id = t.RouteId)
            Dim b = d.Buses.Single(Function(x) x.Id = t.BusId)
            Dim statusColor = Ui.StatusColor(t, b.Capacity)
            Dim card As New TableLayoutPanel With {.Width = 360, .Height = If(session.Role = "Bookman", 390, 330), .Tag = "scale", .ColumnCount = 1, .BackColor = If(t.Status = "Full", Color.FromArgb(255, 237, 239), If(t.Status = "Loading", Color.FromArgb(230, 241, 255), Color.FromArgb(231, 247, 242))), .Padding = New Padding(20), .Margin = New Padding(10)}
            card.Controls.Add(Ui.LabelText(If(i = 0 AndAlso session.Role = "Bookman" AndAlso t.Status <> "Departed", "NEXT BUS  ·  ", "") & t.Status.ToUpperInvariant(), 10, True, statusColor))
            card.Controls.Add(Ui.LabelText(r.Origin & " → " & r.Destination, 19, True))
            card.Controls.Add(Ui.LabelText(t.Departure.ToString("ddd, dd MMM  ·  HH:mm") & vbCrLf & b.Number & "  ·  " & b.Driver, 10, False, Ui.Muted))
            Dim seatColor = If(t.SeatsSold >= b.Capacity, Ui.Red, If(CDec(t.SeatsSold) / b.Capacity >= 0.9D, Ui.Amber, Ui.Green))
            card.Controls.Add(Ui.LabelText((b.Capacity - t.SeatsSold).ToString() & " seats left", 23, True, seatColor))
            card.Controls.Add(Ui.LabelText(t.SeatsSold & " / " & b.Capacity & " sold    ·    GHS " & r.Fare.ToString("N2"), 11, True))
            Dim actions = Ui.Flow()
            actions.Padding = New Padding(0)
            Dim sale = Ui.ButtonText(If(session.Role = "Passenger", "Book ticket", "Sell at counter"), Sub() SellTicket(t.Id), If(session.Role = "Passenger", Ui.Blue, Ui.Green))
            sale.Enabled = t.Status <> "Departed" AndAlso t.Status <> "Full" AndAlso t.SeatsSold < b.Capacity
            If Not sale.Enabled Then sale.Text = If(t.Status = "Departed", "Departed", "Full · sales closed")
            actions.Controls.Add(sale)
            If session.Role = "Bookman" Then
                If t.Status = "Scheduled" Then actions.Controls.Add(Ui.ButtonText("Start Loading", Sub() ChangeTrip(t.Id, "Loading"), Ui.Blue))
                If {"Loading", "Full"}.Contains(t.Status) Then actions.Controls.Add(Ui.ButtonText("Depart bus", Sub() ChangeTrip(t.Id, "Departed"), Ui.Muted))
            End If
            card.Controls.Add(actions)
            cards.Controls.Add(card)
        Next
        cards.ResumeLayout()
    End Sub

    Private Sub ChangeTrip(id As String, status As String)
        If status = "Departed" AndAlso MessageBox.Show("Depart this bus and close sales / boarding?", "Departure", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
        DataStore.ChangeStatus(session, id, status)
        RefreshData(True)
    End Sub

    Private Sub SellTicket(id As String)
        busy = True
        Try
            Dim d = DataStore.Snapshot(session)
            Dim t = d.Trips.Single(Function(x) x.Id = id)
            Dim r = d.Routes.Single(Function(x) x.Id = t.RouteId)
            Dim u = d.Users.Single(Function(x) x.Id = session.UserId)
            Using editor As New EntryDialog(If(session.Role = "Passenger", "Book your journey", "Counter sale"), "Issue ticket")
                editor.AddNote(r.Origin & " → " & r.Destination & "  ·  " & t.Departure.ToString("dd MMM HH:mm") & "  ·  GHS " & r.Fare.ToString("N2"))
                editor.AddText("name", "Passenger name", If(session.Role = "Passenger", u.Name, ""))
                editor.AddText("phone", "Ghana phone", If(session.Role = "Passenger", u.Phone, ""))
                editor.AddChoice("method", "Payment", If(session.Role = "Passenger", {New Choice("MoMo", "MoMo (simulated)"), New Choice("Pay at station", "Pay at station (seat reserved)")}, {New Choice("Cash", "Cash received"), New Choice("MoMo", "MoMo (simulated)")}))
                editor.AddNote("A reserved ticket uses one seat. Pay at station tickets must be paid before boarding. No real money or SMS is sent.")
                editor.SubmitAction = Sub()
                                          DataStore.ValidatePerson(editor.TextValue("name"), editor.TextValue("phone"))
                                          Dim method = editor.ChoiceValue("method")
                                          Dim approved = False
                                          If method = "MoMo" Then
                                              Using payment As New MomoDialog(editor.TextValue("phone"), r.Fare)
                                                  If payment.ShowDialog(editor) <> DialogResult.OK Then Throw New ArgumentException("Mock payment cancelled. No ticket was created.")
                                                  approved = True
                                              End Using
                                          End If
                                          ' Capacity is checked again here after the simulated payment delay.
                                          Dim ticket = DataStore.Sell(session, id, editor.TextValue("name"), editor.TextValue("phone"), method, approved)
                                          MessageBox.Show("SMS sent to " & ticket.Phone & ": your ticket code is " & ticket.Number & vbCrLf & "Simulation only · recorded in smslog.json", "Mock SMS sent", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                          Using view As New TicketForm(session, ticket.Number)
                                              view.ShowDialog(editor)
                                          End Using
                                      End Sub
                editor.ShowDialog(Me)
            End Using
        Finally
            busy = False
            RefreshData(True)
        End Try
    End Sub

    Private Sub ShowExistingTicket(number As String)
        busy = True
        Try
            Using view As New TicketForm(session, number)
                view.ShowDialog(Me)
            End Using
        Finally
            busy = False
        End Try
    End Sub

    Private Sub RenderTickets(d As AppData, title As String)
        Dim tickets = d.Tickets.Where(Function(x) If(session.Role = "Passenger", x.UserId = session.UserId, d.Routes.Single(Function(r) r.Id = d.Trips.Single(Function(t) t.Id = x.TripId).RouteId).Origin = session.Station)).OrderByDescending(Function(x) x.Created)
        Ui.Bind(grids(title), Ui.MakeTable({"Ticket", "Passenger", "Route", "Departure:date", "Fare (GHS):money", "Payment", "Ticket status", "Bus status"}, tickets.Select(Function(x)
                                                                                                                                                                                        Dim t = d.Trips.Single(Function(a) a.Id = x.TripId)
                                                                                                                                                                                        Dim r = d.Routes.Single(Function(a) a.Id = t.RouteId)
                                                                                                                                                                                        Return New Object() {x.Number, x.Passenger, r.Origin & " → " & r.Destination, t.Departure, x.Amount, x.PaymentMethod, x.Status, t.Status}
                                                                                                                                                                                    End Function)))
    End Sub

    Private Sub RenderOperations(d As AppData, title As String)
        Ui.Bind(grids(title), Ui.MakeTable({"Id", "Bus", "From", "Destination", "Departure:date", "Status", "Sold:number", "Capacity:number", "Bookman"}, d.Trips.OrderBy(Function(t) t.Departure).Select(Function(t)
                                                                                                                                                                                                      Dim r = d.Routes.Single(Function(x) x.Id = t.RouteId)
                                                                                                                                                                                                      Dim b = d.Buses.Single(Function(x) x.Id = t.BusId)
                                                                                                                                                                                                      Dim u = d.Users.SingleOrDefault(Function(x) x.Id = t.BookmanId)
                                                                                                                                                                                                      Return New Object() {t.Id, b.Number, r.Origin, r.Destination, t.Departure, t.Status, t.SeatsSold, b.Capacity, If(u Is Nothing, "Unassigned", u.Name)}
                                                                                                                                                                                                  End Function)))
        For Each row As DataGridViewRow In grids(title).Rows
            Dim t = d.Trips.Single(Function(x) x.Id = Convert.ToString(row.Cells("Id").Value))
            row.Cells("Status").Style.ForeColor = Ui.StatusColor(t, d.Buses.Single(Function(b) b.Id = t.BusId).Capacity)
            row.Cells("Status").Style.Font = New Font(Font, FontStyle.Bold)
        Next
    End Sub

    Private Sub RenderOverview(d As AppData, force As Boolean)
        Dim signature = System.Text.Json.JsonSerializer.Serialize(New With {d.Tickets, d.Payments, d.Trips, d.Routes, d.Buses})
        If Not force AndAlso signature = lastOverview Then Return
        lastOverview = signature
        overview.SuspendLayout()
        For Each c In overview.Controls.Cast(Of Control)().ToArray()
            c.Dispose()
        Next
        overview.Controls.Clear()
        Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 1}
        layout.Controls.Add(Ui.LabelText("Today's transport, at a glance", 24, True))
        layout.Controls.Add(Ui.LabelText("Revenue is successful payments less mock refunds. Occupancy counts every active reservation.", 10, False, Ui.Muted))
        Dim metrics = Ui.Flow()
        metrics.Dock = DockStyle.Top
        Dim revenue = d.Payments.Where(Function(p) p.Status = "Successful").Sum(Function(p) p.Amount)
        Dim activeTrips = d.Trips.Where(Function(t) t.Status <> "Departed").ToList()
        Dim occupancy = If(activeTrips.Count = 0, 0D, activeTrips.Average(Function(t) CDec(t.SeatsSold) / d.Buses.Single(Function(b) b.Id = t.BusId).Capacity) * 100D)
        AddMetric(metrics, "TOTAL REVENUE", "GHS " & revenue.ToString("N2"), Ui.Green)
        AddMetric(metrics, "TICKETS SOLD TODAY", d.Tickets.Where(Function(t) t.Created.LocalDateTime.Date = DateTime.Today AndAlso t.Status <> "Cancelled").Count().ToString(), Ui.Blue)
        AddMetric(metrics, "AVERAGE OCCUPANCY", occupancy.ToString("N0") & "%", Ui.Amber)
        AddMetric(metrics, "BUSES LOADING", d.Trips.Where(Function(t) t.Status = "Loading").Count().ToString(), Ui.Blue)
        layout.Controls.Add(metrics)
        layout.Controls.Add(Ui.LabelText("Tickets per route", 18, True))
        Dim chart As New RouteChart(d) With {.Dock = DockStyle.Top, .Height = 250, .Margin = New Padding(8)}
        layout.Controls.Add(chart)
        Dim actions = Ui.Flow()
        actions.Controls.Add(Ui.ButtonText("Refresh dashboard", Sub() RefreshData(True), Ui.Muted))
        actions.Controls.Add(Ui.ButtonText("Reset demo data", Sub()
                                                                If MessageBox.Show("Restore a fresh demo with tomorrow's trips? Current data is backed up in Backups. All users must sign in again.", "Reset demo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) = DialogResult.Yes Then
                                                                    DataStore.ResetDemo(session)
                                                                    Close()
                                                                End If
                                                            End Sub, Ui.Red))
        layout.Controls.Add(actions)
        layout.Controls.Add(Ui.LabelText("Demo tip: the Accra → Kumasi bus has 5 seats. Open two copies from the same folder to show live updates.", 10, False, Ui.Muted))
        overview.Controls.Add(layout)
        overview.ResumeLayout()
    End Sub

    Private Sub AddMetric(flow As FlowLayoutPanel, caption As String, value As String, color As Color)
        Dim card As New FlowLayoutPanel With {.Width = 275, .Height = 145, .Tag = "scale", .BackColor = Color.FromArgb(235, 245, 250), .FlowDirection = FlowDirection.TopDown, .WrapContents = False, .Padding = New Padding(16), .Margin = New Padding(0, 6, 12, 8)}
        card.Controls.Add(Ui.LabelText(caption, 12, True, Ui.Muted))
        card.Controls.Add(Ui.LabelText(value, 28, True, color))
        flow.Controls.Add(card)
    End Sub

    Private Sub EditEntity(kind As String, id As String)
        busy = True
        Try
            AdminEditors.Edit(Me, session, kind, id)
        Finally
            busy = False
            RefreshData(True)
        End Try
    End Sub
End Class

Public Class RouteChart
    Inherits Control
    Private ReadOnly values As List(Of KeyValuePair(Of String, Integer))
    Public Sub New(d As AppData)
        DoubleBuffered = True
        BackColor = Color.White
        values = d.Routes.Select(Function(r) New KeyValuePair(Of String, Integer)(r.Origin & " → " & r.Destination, d.Tickets.Where(Function(t) t.Status <> "Cancelled" AndAlso t.Status <> "Expired" AndAlso d.Trips.Any(Function(tr) tr.Id = t.TripId AndAlso tr.RouteId = r.Id)).Count())).ToList()
        AccessibleName = "Bar chart of active tickets per route"
    End Sub
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim max = Math.Max(1, values.Select(Function(v) v.Value).DefaultIfEmpty(0).Max())
        Dim rowHeight = Math.Max(30, Math.Min(48, (Height - 20) \ Math.Max(1, values.Count)))
        Dim measured = values.Select(Function(v) TextRenderer.MeasureText(v.Key, Font).Width).DefaultIfEmpty(180).Max()
        Dim labelWidth = Math.Min(Width \ 2, measured + 28)
        For i As Integer = 0 To values.Count - 1
            Dim y = 12 + i * rowHeight
            TextRenderer.DrawText(e.Graphics, values(i).Key, Font, New Rectangle(12, y, labelWidth - 16, rowHeight), Ui.Ink, TextFormatFlags.EndEllipsis)
            Dim widthAvailable = Math.Max(20, Width - labelWidth - 65)
            Using track As New SolidBrush(Ui.Paper), fill As New SolidBrush(Ui.Blue)
                e.Graphics.FillRectangle(track, labelWidth, y + 2, widthAvailable, 22)
                e.Graphics.FillRectangle(fill, labelWidth, y + 2, CInt(widthAvailable * values(i).Value / max), 22)
            End Using
            TextRenderer.DrawText(e.Graphics, values(i).Value.ToString(), Font, New Point(labelWidth + widthAvailable + 12, y), Ui.Ink)
        Next
    End Sub
End Class
