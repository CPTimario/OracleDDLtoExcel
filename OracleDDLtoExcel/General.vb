﻿Public Module General
    '-----------
    ' Constants
    '-----------
    Public Const SINGLE_COMMENT_START = "--"
    Public Const SINGLE_COMMENT_END = vbCrLf
    Public Const MULTI_COMMENT_START = "/*"
    Public Const MULTI_COMMENT_END = "*/"

    '------------
    ' Structures
    '------------
    Public Structure Constraint
        Dim Type As ConstraintType
        Dim Expression As String
        Dim Reference As KeyValuePair(Of String, String)

        Public Sub New(ByVal pType As ConstraintType)
            Type = pType
        End Sub

        Public Sub New(ByVal pType As ConstraintType, ByVal pRefTable As String, ByVal pRefColumn As String)
            Type = pType
            Reference = New KeyValuePair(Of String, String)(pRefTable, pRefColumn)
        End Sub

        Public Sub New(ByVal pType As ConstraintType, ByVal pExpression As String)
            Type = pType
            Expression = pExpression
        End Sub
    End Structure

    '--------------
    ' Enumerations
    '--------------
    Public Enum ConstraintType
        ctNOTNULL
        ctPRIMARY
        ctUNIQUE
        ctFOREIGN
        ctCHECK
    End Enum

    Public Enum DataTypes
        dtCHAR
        dtVARCHAR2
        dtNCHAR
        dtNVARCHAR2
        dtLONG
        dtNUMBER
        dtDATE
    End Enum

    '------------
    ' Functions
    '------------
    Public Function RemoveComments(ByVal pString As String) As String
        Dim resultString As String = pString

        resultString = RemoveSubstring(resultString, SINGLE_COMMENT_START, SINGLE_COMMENT_END)
        resultString = RemoveSubstring(resultString, MULTI_COMMENT_START, MULTI_COMMENT_END)

        Return resultString
    End Function
End Module
