Imports System.Drawing.Printing
Imports System.IO
Imports QRCoder

Public Class MomoDialog
    Inherits Form
    Private ReadOnly waitTimer As New Timer With {.Interval = 2600}
    Private ReadOnly status As Label = Ui.LabelText("Approve payment on your phone", 19, True)
    Public Sub New(phone As String, amount As Decimal)
        Ui.Style(Me, "Mock Mobile Money", 620, 380)
        MinimumSize = New Size(560, 360)
        ControlBox = False
        Dim content As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .WrapContents = False, .Padding = New Padding(30)}
        content.Controls.Add(Ui.LabelText("MOBILE MONEY  /  SIMULATION", 11, True, Ui.Blue))
        content.Controls.Add(status)
        content.Controls.Add(Ui.LabelText("GHS " & amount.ToString("N2") & "  ·  " & phone, 16, True))
        content.Controls.Add(Ui.LabelText("Waiting for simulated approval…" & vbCrLf & "No real charge will be made.", 11))
        content.Controls.Add(New ProgressBar With {.Style = ProgressBarStyle.Marquee, .Width = 490, .Height = 12})
        content.Controls.Add(Ui.ButtonText("Cancel", Sub()
                                                      waitTimer.Stop()
                                                      DialogResult = DialogResult.Cancel
                                                      Close()
                                                  End Sub, Ui.Muted))
        Controls.Add(content)
        AddHandler Shown, Sub() waitTimer.Start()
        AddHandler waitTimer.Tick, Sub()
                                       waitTimer.Stop()
                                       DialogResult = DialogResult.OK
                                       Close()
                                   End Sub
        AddHandler FormClosed, Sub() waitTimer.Dispose()
    End Sub
End Class

Public Class TicketForm
    Inherits Form
    Private ReadOnly ticket As Ticket
    Private ReadOnly journey As Trip
    Private ReadOnly route As BusRoute
    Private ReadOnly qr As Bitmap
    Private ReadOnly payload As String

    Public Sub New(session As LoginSession, number As String)
        payload = DataStore.QrText(session, number)
        Dim d = DataStore.Snapshot(session)
        ticket = d.Tickets.Single(Function(t) t.Number = number)
        journey = d.Trips.Single(Function(t) t.Id = ticket.TripId)
        route = d.Routes.Single(Function(r) r.Id = journey.RouteId)
        Using generator As New QRCodeGenerator(), data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q), renderer As New PngByteQRCode(data), stream As New MemoryStream(renderer.GetGraphic(6))
            Using sourceImage = Image.FromStream(stream)
                qr = New Bitmap(sourceImage)
            End Using
        End Using
        Ui.Style(Me, "Digital ticket", 730, 790)
        MinimumSize = New Size(650, 650)
        Dim body As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .AutoScroll = True, .Padding = New Padding(28), .BackColor = Color.White}
        body.Controls.Add(New PictureBox With {.Image = Ui.BrandLogo(), .BackColor = Color.White, .SizeMode = PictureBoxSizeMode.Zoom, .Size = New Size(72, 72), .Tag = "scale"})
        body.Controls.Add(Ui.LabelText("GROUP1 TRANSPORT", 24, True, Ui.Blue))
        body.Controls.Add(Ui.LabelText(route.Origin & " → " & route.Destination, 24, True))
        body.Controls.Add(Ui.LabelText(journey.Departure.ToString("dddd, dd MMMM yyyy  ·  HH:mm"), 13, True))
        body.Controls.Add(Ui.LabelText("PASSENGER  " & ticket.Passenger & vbCrLf & "FARE  GHS " & ticket.Amount.ToString("N2") & "    ·    " & ticket.PaymentMethod & vbCrLf & "STATUS  " & ticket.Status & "    ·    BUS  " & journey.Status, 11))
        body.Controls.Add(Ui.LabelText(ticket.Number, 24, True))
        body.Controls.Add(New PictureBox With {.Image = qr, .Width = 250, .Height = 250, .Tag = "scale", .SizeMode = PictureBoxSizeMode.Zoom, .Margin = New Padding(0, 0, 0, 0), .AccessibleName = "Signed boarding QR for " & ticket.Number})
        body.Controls.Add(Ui.LabelText("One trip · one boarding · seat count only" & vbCrLf & If(ticket.Status = "Booked", "PAYMENT PENDING: pay at the station before boarding.", "Present this ticket at the origin station."), 10, True, If(ticket.Status = "Booked", Ui.Amber, Ui.Green)))
        Dim buttons = Ui.Flow()
        buttons.Controls.Add(Ui.ButtonText("Print preview", AddressOf Preview))
        buttons.Controls.Add(Ui.ButtonText("Copy QR text", Sub()
                                                             Clipboard.SetText(payload)
                                                             MessageBox.Show("Signed QR text copied. Paste it into Validate boarding.", "Copied")
                                                         End Sub, Ui.Green))
        buttons.Controls.Add(Ui.ButtonText("Close", Sub() Close(), Ui.Muted))
        body.Controls.Add(buttons)
        Controls.Add(body)
        AddHandler FormClosed, Sub() qr.Dispose()
    End Sub

    Private Sub Preview()
        Using doc As New PrintDocument()
            doc.DocumentName = "Group1 ticket " & ticket.Number
            doc.DefaultPageSettings.Margins = New Margins(45, 45, 45, 45)
            AddHandler doc.PrintPage, AddressOf DrawTicket
            Using previewDialog As New PrintPreviewDialog With {.Document = doc, .Width = 1000, .Height = 760, .StartPosition = FormStartPosition.CenterParent, .UseAntiAlias = True}
                previewDialog.ShowDialog(Me)
            End Using
        End Using
    End Sub

    Private Sub DrawTicket(sender As Object, e As PrintPageEventArgs)
        Dim g = e.Graphics
        If g Is Nothing Then Return
        Dim x = e.MarginBounds.Left
        Dim y = e.MarginBounds.Top
        Using heading As New Font("Segoe UI", 24, FontStyle.Bold), body As New Font("Segoe UI", 12), bold As New Font("Segoe UI", 16, FontStyle.Bold), brush As New SolidBrush(Ui.Ink)
            g.DrawString("GROUP1 TRANSPORT", heading, brush, x, y)
            g.DrawString(route.Origin & " → " & route.Destination, bold, brush, x, y + 55)
            g.DrawString(journey.Departure.ToString("dddd, dd MMMM yyyy HH:mm"), body, brush, x, y + 95)
            g.DrawString("Passenger: " & ticket.Passenger & vbCrLf & "Phone: " & ticket.Phone & vbCrLf & "Fare: GHS " & ticket.Amount.ToString("N2") & " | " & ticket.PaymentMethod & vbCrLf & "Status: " & ticket.Status, body, brush, x, y + 140)
            g.DrawString(ticket.Number, bold, brush, x, y + 245)
            g.DrawImage(qr, x, y + 290, 250, 250)
            g.DrawString("One trip. One boarding. No numbered seats." & vbCrLf & "Classroom demo · payments and SMS simulated.", body, brush, x, y + 570)
        End Using
        e.HasMorePages = False
    End Sub
End Class
