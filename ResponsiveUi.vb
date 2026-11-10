Imports System.Runtime.CompilerServices

' Scale readable type and the controls that contain it together. Reflow, never stretch text boxes across a monitor.
Public Module ResponsiveUi
    Private ReadOnly controllers As New ConditionalWeakTable(Of Form, ResizeController)
    Public Sub Attach(form As Form)
        controllers.GetValue(form, Function(f) New ResizeController(f))
    End Sub
    Public Sub Apply(form As Form)
        Dim controller As ResizeController = Nothing
        If controllers.TryGetValue(form, controller) Then controller.Apply()
    End Sub

    Private Class Baseline
        Public Property Size As Single
        Public Property Width As Integer
        Public Property Height As Integer
        Public Property FontStyle As FontStyle
        Public Property Family As String
        Public Property Factor As Single = -1
        Public Property AssignedFont As Font
    End Class

    Private Class ResizeController
        Private ReadOnly form As Form
        Private ReadOnly styles As New Dictionary(Of Control, Baseline)
        Private ReadOnly debounce As New Timer With {.Interval = 100}
        Private applying As Boolean
        Public Sub New(owner As Form)
            form = owner
            AddHandler form.Shown, Sub() Apply()
            AddHandler form.SizeChanged, Sub()
                                             If Not form.Visible Then Return
                                             debounce.Stop()
                                             debounce.Start()
                                         End Sub
            AddHandler debounce.Tick, Sub()
                                          debounce.Stop()
                                          Apply()
                                      End Sub
            AddHandler form.FormClosed, Sub()
                                            debounce.Dispose()
                                            For Each s In styles.Values
                                                s.AssignedFont?.Dispose()
                                            Next
                                            styles.Clear()
                                        End Sub
        End Sub

        Private Sub Capture(c As Control)
            If Not styles.ContainsKey(c) Then
                styles(c) = New Baseline With {.Size = c.Font.Size, .FontStyle = c.Font.Style, .Family = c.Font.FontFamily.Name, .Width = c.Width, .Height = c.Height}
            End If
            If TypeOf c Is DataGridView Then Return
            For Each child As Control In c.Controls
                Capture(child)
            Next
        End Sub

        Public Sub Apply()
            If applying OrElse form.IsDisposed OrElse Not form.IsHandleCreated Then Return
            applying = True
            Try
                For Each removed In styles.Keys.Where(Function(c) c.IsDisposed).ToList()
                    styles(removed).AssignedFont?.Dispose()
                    styles.Remove(removed)
                Next
                Capture(form)
                Dim factor = CSng(Math.Clamp(form.ClientSize.Width / 1120.0, 1.0, 1.45)) * If(Ui.PresentationMode, 1.25F, 1.0F)
                form.SuspendLayout()
                For Each pair In styles
                    Dim c = pair.Key
                    Dim b = pair.Value
                    If Math.Abs(b.Factor - factor) < 0.01F Then Continue For
                    Dim font As New Font(b.Family, Math.Max(12, b.Size) * factor, b.FontStyle)
                    Dim oldFont = b.AssignedFont
                    c.Font = font
                    b.AssignedFont = font
                    b.Factor = factor
                    oldFont?.Dispose()
                    If Convert.ToString(c.Tag) = "scale" Then
                        c.Size = New Size(CInt(b.Width * factor), CInt(b.Height * factor))
                    ElseIf (TypeOf c Is TextBox OrElse TypeOf c Is ComboBox) AndAlso c.Dock = DockStyle.None Then
                        c.Width = CInt(b.Width * factor)
                    End If
                    If TypeOf c Is Button Then c.MinimumSize = New Size(CInt(125 * factor), CInt(48 * factor))
                    Dim grid = TryCast(c, DataGridView)
                    If grid IsNot Nothing Then
                        grid.ColumnHeadersDefaultCellStyle.Font = New Font(font, FontStyle.Bold)
                        grid.ColumnHeadersHeight = CInt(52 * factor)
                        grid.RowTemplate.Height = CInt(45 * factor)
                    End If
                Next
                ConstrainLabels(form)
                form.ResumeLayout(True)
            Finally
                applying = False
            End Try
        End Sub

        Private Sub ConstrainLabels(parent As Control)
            If TypeOf parent Is DataGridView Then Return
            For Each c As Control In parent.Controls
                If TypeOf c Is Label AndAlso parent.ClientSize.Width > 150 Then
                    c.MaximumSize = New Size(Math.Max(100, parent.ClientSize.Width - parent.Padding.Horizontal - 20), 0)
                End If
                If TypeOf parent Is FlowLayoutPanel AndAlso DirectCast(parent, FlowLayoutPanel).FlowDirection = FlowDirection.TopDown AndAlso (TypeOf c Is TextBox OrElse TypeOf c Is CheckBox) Then
                    c.MaximumSize = New Size(Math.Max(150, parent.ClientSize.Width - parent.Padding.Horizontal - 20), 0)
                End If
                ConstrainLabels(c)
            Next
        End Sub
    End Class
End Module
