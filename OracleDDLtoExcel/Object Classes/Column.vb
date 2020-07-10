﻿Public Class Column
    '------------
    ' Attributes
    '------------
    Public Name As String
    Public DataType As DataType
    Public DefaultValue As String
    Public Constraints As List(Of Constraint)
    Public Comment As String

    '-------------
    ' Constructor
    '-------------
    Public Sub New(ByVal name As String, ByVal dataType As DataType, Optional ByVal defaultValue As String = "", Optional ByVal constraints As List(Of Constraint) = Nothing)
        Me.Name = name
        Me.DataType = dataType
        Me.DefaultValue = defaultValue
        Me.Constraints = IIf(constraints Is Nothing, New List(Of Constraint), constraints)
    End Sub

    '---------
    ' Methods
    '---------
    Public Sub AddConstraint(ByVal type As Constraint._Type, Optional ByVal addlClauses As String() = Nothing)
        If Not Constraints.Exists(Function(ct) ct.Type = type) Then
            If type = Constraint._Type._FOREIGN Then
                Constraints.Add(New Constraint(type, addlClauses(0), addlClauses(1)))
            ElseIf type = Constraint._Type._CHECK Then
                Constraints.Add(New Constraint(type, addlClauses(0)))
            Else
                Constraints.Add(New Constraint(type))
            End If
        End If
    End Sub

    Public Sub AddComment(ByVal comment As String)
        Me.Comment = comment
    End Sub
End Class
