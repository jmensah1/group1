Public Class UserAccount
    Public Property Id As String = Guid.NewGuid().ToString("N")
    Public Property Username As String = ""
    Public Property PasswordHash As String = ""
    Public Property Salt As String = ""
    Public Property Role As String = "Passenger"
    Public Property Name As String = ""
    Public Property Phone As String = ""
    Public Property Station As String = "Accra"
    Public Property Active As Boolean = True
    Public Property FailedAttempts As Integer
    Public Property LockedUntil As DateTimeOffset = DateTimeOffset.MinValue
End Class

Public Class Bus
    Public Property Id As String = Guid.NewGuid().ToString("N")
    Public Property Number As String = ""
    Public Property Capacity As Integer = 45
    Public Property Driver As String = ""
End Class

Public Class BusRoute
    Public Property Id As String = Guid.NewGuid().ToString("N")
    Public Property Origin As String = "Accra"
    Public Property Destination As String = "Kumasi"
    Public Property Fare As Decimal = 150D
    Public Property Distance As Integer = 250
End Class

Public Class Trip
    Public Property Id As String = Guid.NewGuid().ToString("N")
    Public Property RouteId As String = ""
    Public Property BusId As String = ""
    Public Property Departure As DateTime = DateTime.Today.AddDays(1).AddHours(8)
    Public Property Status As String = "Scheduled"
    Public Property ResumeStatus As String = "Scheduled"
    Public Property SeatsSold As Integer
    Public Property BookmanId As String = ""
End Class

Public Class Ticket
    Public Property Number As String = ""
    Public Property TripId As String = ""
    Public Property UserId As String = ""
    Public Property Passenger As String = ""
    Public Property Phone As String = ""
    Public Property PaymentMethod As String = ""
    Public Property Status As String = "Booked"
    Public Property Amount As Decimal
    Public Property Created As DateTimeOffset = DateTimeOffset.Now
End Class

Public Class Payment
    Public Property Ticket As String = ""
    Public Property Method As String = ""
    Public Property Amount As Decimal
    Public Property Status As String = "Pending"
    Public Property Time As DateTimeOffset = DateTimeOffset.Now
End Class

Public Class SmsEntry
    Public Property Phone As String = ""
    Public Property Message As String = ""
    Public Property Time As DateTimeOffset = DateTimeOffset.Now
End Class

Public Class AuditEntry
    Public Property User As String = ""
    Public Property Action As String = ""
    Public Property Details As String = ""
    Public Property Time As DateTimeOffset = DateTimeOffset.Now
End Class

Public Class AppData
    Public Property Users As New List(Of UserAccount)
    Public Property Stations As New List(Of String)
    Public Property Routes As New List(Of BusRoute)
    Public Property Buses As New List(Of Bus)
    Public Property Trips As New List(Of Trip)
    Public Property Tickets As New List(Of Ticket)
    Public Property Payments As New List(Of Payment)
    Public Property SmsLog As New List(Of SmsEntry)
    Public Property AuditLog As New List(Of AuditEntry)
    Public Property SigningKey As String = ""
End Class

Public Class LoginSession
    Friend Property Token As String = Guid.NewGuid().ToString("N")
    Public Property UserId As String
    Public Property Username As String
    Public Property Name As String
    Public Property Role As String
    Public Property Station As String
End Class
