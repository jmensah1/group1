Imports System.Data

Public Module Ui
    Public ReadOnly Ink As Color = Color.FromArgb(16, 44, 70)
    Public ReadOnly Muted As Color = Color.FromArgb(72, 91, 111)
    Public ReadOnly Paper As Color = Color.FromArgb(242, 246, 250)
    Public ReadOnly Blue As Color = Color.FromArgb(21, 101, 192)
    Public ReadOnly Green As Color = Color.FromArgb(0, 124, 108)
    Public ReadOnly Gold As Color = Color.FromArgb(250, 196, 74)
    Public Property PresentationMode As Boolean
    Public ReadOnly Amber As Color = Color.FromArgb(150, 93, 0)
    Public ReadOnly Red As Color = Color.FromArgb(186, 39, 52)

    Public Sub Style(form As Form, title As String, Optional width As Integer = 1120, Optional height As Integer = 780)
        form.Text = title & " | Group1 Transport"
        form.Font = New Font("Segoe UI", 14)
        form.BackColor = Paper
        form.ForeColor = Ink
        form.StartPosition = FormStartPosition.CenterScreen
        form.MinimumSize = New Size(Math.Min(900, width + 16), Math.Min(680, height + 40))
        form.ClientSize = New Size(width, height)
        form.AutoScaleMode = AutoScaleMode.Dpi
        form.AutoScaleDimensions = New SizeF(96, 96)
        ResponsiveUi.Attach(form)
    End Sub

    Public Function LabelText(text As String, Optional size As Single = 10, Optional bold As Boolean = False, Optional color As Color = Nothing) As Label
        Return New Label With {.Text = text, .AutoSize = True, .Font = New Font("Segoe UI", Math.Max(12, size), If(bold, FontStyle.Bold, FontStyle.Regular)), .ForeColor = If(color = Nothing, Ink, color), .Margin = New Padding(0, 4, 12, 8)}
    End Function

    Public Function ButtonText(text As String, action As Action, Optional color As Color = Nothing) As Button
        Dim b As New Button With {.Text = text, .Font = New Font("Segoe UI", 14, FontStyle.Bold), .AutoSize = True, .MinimumSize = New Size(125, 48), .Padding = New Padding(12, 6, 12, 6), .BackColor = If(color = Nothing, Blue, color), .ForeColor = If(color = Gold, Ink, Color.White), .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand, .Margin = New Padding(0, 4, 10, 4)}
        b.FlatAppearance.BorderSize = 0
        AddHandler b.Click, Sub(s, e) Guard(action)
        Return b
    End Function

    Public Sub Guard(action As Action)
        Try
            action()
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Please check", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Public Function Flow(Optional fill As Boolean = False) As FlowLayoutPanel
        Return New FlowLayoutPanel With {.Dock = If(fill, DockStyle.Fill, DockStyle.Top), .AutoSize = Not fill, .AutoScroll = fill, .WrapContents = True, .Padding = New Padding(8), .BackColor = Color.Transparent}
    End Function

    Public Function Grid() As DataGridView
        Dim g As New DataGridView With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells, .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None, .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .MultiSelect = False, .EnableHeadersVisualStyles = False}
        g.DefaultCellStyle.Padding = New Padding(5)
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 233, 250)
        g.DefaultCellStyle.SelectionForeColor = Ink
        g.ColumnHeadersDefaultCellStyle.BackColor = Ink
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        g.Font = New Font("Segoe UI", 14)
        g.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 14, FontStyle.Bold)
        g.ColumnHeadersHeight = 50
        g.DefaultCellStyle.WrapMode = DataGridViewTriState.True
        g.AlternatingRowsDefaultCellStyle.BackColor = Paper
        Return g
    End Function

    Public Function MakeTable(columns As String(), rows As IEnumerable(Of Object())) As DataTable
        Dim dt As New DataTable
        For Each c In columns
            Dim parts = c.Split(":"c)
            Dim kind As Type = GetType(String)
            If parts.Length > 1 Then
                Select Case parts(1)
                    Case "number" : kind = GetType(Integer)
                    Case "money" : kind = GetType(Decimal)
                    Case "date" : kind = GetType(DateTime)
                End Select
            End If
            dt.Columns.Add(parts(0), kind)
        Next
        For Each row In rows
            dt.Rows.Add(row)
        Next
        Return dt
    End Function

    Public Sub Bind(g As DataGridView, table As DataTable)
        Dim selected As String = Nothing
        If g.CurrentRow IsNot Nothing AndAlso g.Columns.Count > 0 Then selected = Convert.ToString(g.CurrentRow.Cells(0).Value)
        Dim sorting = TryCast(g.DataSource, DataTable)?.DefaultView.Sort
        If Not String.IsNullOrEmpty(sorting) Then table.DefaultView.Sort = sorting
        g.DataSource = table
        If g.Columns.Contains("Id") Then g.Columns("Id").Visible = False
        For Each col As DataGridViewColumn In g.Columns
            col.SortMode = DataGridViewColumnSortMode.Automatic
            col.MinimumWidth = 120
            If table.Columns(col.Name).DataType = GetType(Decimal) Then col.DefaultCellStyle.Format = "N2"
            If table.Columns(col.Name).DataType = GetType(DateTime) Then col.DefaultCellStyle.Format = "dd MMM yyyy HH:mm"
        Next
        If selected IsNot Nothing Then
            For Each row As DataGridViewRow In g.Rows
                If Convert.ToString(row.Cells(0).Value) = selected Then
                    g.CurrentCell = row.Cells.Cast(Of DataGridViewCell)().First(Function(c) c.Visible)
                    Exit For
                End If
            Next
        End If
    End Sub

    Public Function BrandLogo() As Image
        Return DirectCast(New System.ComponentModel.ComponentResourceManager(GetType(Form1)).GetObject("logoPicture.Image"), Image)
    End Function

    Public Sub TogglePresentation()
        PresentationMode = Not PresentationMode
        For Each form As Form In Application.OpenForms
            ResponsiveUi.Apply(form)
        Next
    End Sub

    Public Function SelectedId(g As DataGridView) As String
        If g.CurrentRow Is Nothing Then Throw New ArgumentException("Select a row first.")
        Return Convert.ToString(g.CurrentRow.Cells(0).Value)
    End Function

    Public Function StatusColor(t As Trip, capacity As Integer) As Color
        If t.Status = "Departed" Then Return Muted
        If t.Status = "Full" OrElse t.SeatsSold >= capacity Then Return Red
        If CDec(t.SeatsSold) / capacity >= 0.9D Then Return Amber
        If t.Status = "Loading" Then Return Blue
        Return Green
    End Function
End Module

Public Class Choice
    Public Property Id As String
    Public Property Caption As String
    Public Sub New(id As String, caption As String)
        Me.Id = id
        Me.Caption = caption
    End Sub
    Public Overrides Function ToString() As String
        Return Caption
    End Function
End Class

' Reusable responsive form editor, used by registration, sales and all admin editors.
Public Class EntryDialog
    Inherits Form
    Private ReadOnly fields As New Dictionary(Of String, Control)
    Private Shadows ReadOnly layout As New TableLayoutPanel With {.AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowOnly, .ColumnCount = 2, .Padding = New Padding(24)}
    Private ReadOnly scrollPanel As New Panel With {.Dock = DockStyle.Fill, .AutoScroll = True}
    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property SubmitAction As Action
    Public ReadOnly Property SubmitButton As Button

    Public Sub New(title As String, Optional submitText As String = "Save changes")
        Ui.Style(Me, title, 660, 570)
        MinimumSize = New Size(590, 480)
        ShowInTaskbar = False
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 34))
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 66))
        scrollPanel.Controls.Add(layout)
        Controls.Add(scrollPanel)
        Dim footer = Ui.Flow()
        footer.Dock = DockStyle.Bottom
        SubmitButton = Ui.ButtonText(submitText, Sub()
                                                     If SubmitAction IsNot Nothing Then SubmitAction.Invoke()
                                                     DialogResult = DialogResult.OK
                                                     Close()
                                                 End Sub)
        footer.Controls.Add(SubmitButton)
        footer.Controls.Add(Ui.ButtonText("Close", Sub() Close(), Ui.Muted))
        Controls.Add(footer)
        AddNote(title, True)
        AddHandler Shown, Sub() ArrangeEditor()
        AddHandler Resize, Sub() ArrangeEditor()
    End Sub

    Private Sub ArrangeEditor()
        If scrollPanel.ClientSize.Width < 100 Then Return
        layout.Width = Math.Min(1000, scrollPanel.ClientSize.Width - 16)
        layout.Location = New Point(Math.Max(0, (scrollPanel.ClientSize.Width - layout.Width) \ 2), 0)
    End Sub

    Public Sub AddNote(text As String, Optional heading As Boolean = False)
        Dim l = Ui.LabelText(text, If(heading, 18, 10), heading)
        l.MaximumSize = New Size(540, 0)
        layout.Controls.Add(l, 0, layout.RowCount)
        layout.SetColumnSpan(l, 2)
        layout.RowCount += 1
    End Sub

    Private Sub AddField(key As String, caption As String, c As Control)
        c.Dock = DockStyle.Top
        c.Margin = New Padding(0, 8, 0, 10)
        layout.Controls.Add(Ui.LabelText(caption), 0, layout.RowCount)
        layout.Controls.Add(c, 1, layout.RowCount)
        layout.RowCount += 1
        fields.Add(key, c)
    End Sub

    Public Sub AddText(key As String, caption As String, Optional value As String = "", Optional password As Boolean = False)
        AddField(key, caption, New TextBox With {.Font = New Font("Segoe UI", 16), .Text = value, .UseSystemPasswordChar = password, .MaxLength = If(password, 128, 150)})
    End Sub

    Public Sub AddChoice(key As String, caption As String, choices As IEnumerable(Of Choice), Optional selected As String = "")
        Dim combo As New ComboBox With {.Font = New Font("Segoe UI", 14), .DropDownStyle = ComboBoxStyle.DropDownList}
        For Each item In choices
            combo.Items.Add(item)
            If item.Id = selected Then combo.SelectedItem = item
        Next
        If combo.SelectedIndex = -1 AndAlso combo.Items.Count > 0 Then combo.SelectedIndex = 0
        AddField(key, caption, combo)
    End Sub

    Public Sub AddNumber(key As String, caption As String, value As Decimal, min As Decimal, max As Decimal, Optional decimals As Integer = 0)
        AddField(key, caption, New NumericUpDown With {.Minimum = min, .Maximum = max, .DecimalPlaces = decimals, .Value = Math.Clamp(value, min, max), .ThousandsSeparator = True})
    End Sub

    Public Sub AddDate(key As String, caption As String, value As DateTime)
        AddField(key, caption, New DateTimePicker With {.Format = DateTimePickerFormat.Custom, .CustomFormat = "dd MMM yyyy HH:mm", .Value = value, .ShowUpDown = False})
    End Sub

    Public Sub AddCheck(key As String, caption As String, value As Boolean)
        AddField(key, caption, New CheckBox With {.Checked = value, .Text = "Yes", .AutoSize = True})
    End Sub

    Public Function TextValue(key As String) As String
        Return fields(key).Text.Trim()
    End Function
    Public Function ChoiceValue(key As String) As String
        Dim value = TryCast(DirectCast(fields(key), ComboBox).SelectedItem, Choice)
        If value Is Nothing Then Throw New ArgumentException("Choose " & key & ".")
        Return value.Id
    End Function
    Public Function NumberValue(key As String) As Decimal
        Return DirectCast(fields(key), NumericUpDown).Value
    End Function
    Public Function DateValue(key As String) As DateTime
        Return DirectCast(fields(key), DateTimePicker).Value
    End Function
    Public Function CheckValue(key As String) As Boolean
        Return DirectCast(fields(key), CheckBox).Checked
    End Function
End Class
