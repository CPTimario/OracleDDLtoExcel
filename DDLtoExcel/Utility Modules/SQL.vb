﻿Imports System.Text.RegularExpressions

Public Class SQL
    '-----------
    ' Variables
    '-----------
    Public Schemas As List(Of Schema)
    Private CurrentSchema As Schema

    '-----------
    ' Constants
    '-----------
    Public Structure SQLRegex
        Const SQL_COMMENT As String = "(?:--[^\n]*)|(?:\/\*[^\*\/]*\*\/)"

        Const TABLE_NAME As String = "(?:[\""\']?(\w+)[\""\']?\.)?[\""\']?(\w+)[\""\']?"
        Const COLUMN_NAME As String = "[\""\']?(\w+)[\""\']?"
        Const DATA_TYPE As String = "(?!(?:DEFAULT|AUTO\s+INCREMENT))\w*)\s*(\([\w\s\,]+\))?"
        Const CONSTRAINT_NAME As String = "[\""\']?(\w+)[\""\']?"

        Const CREATE_COLUMN_LIST As String = "\(\s*((" & CREATE_COLUMN_SYNTAX & "[\s\,]+)+)\)"
        Const CREATE_COLUMN_SYNTAX As String = COLUMN_NAME & "\s+(\w+\s*" & DATA_TYPE & "\s*((?:DEFAULT\s+([^\n]+))|(?:AUTO INCREMENT))?"

        Const ALTER_COLUMN_LIST = "\(\s*((?:[\""\']?\w+[\""\']?\,*\s*)+)\s*\)"
        Const ALTER_ADD_CONSTRAINT = "\s+ADD\s+CONSTRAINT\s+" & CONSTRAINT_NAME

        Const COMMENT_ON As String = "\s+IS\s+[\""\']?([\w一-龠ぁ-ゔァ-ヴーａ-ｚＡ-Ｚ０-９々〆〤]+)[\""\']?"
    End Structure

    '--------------
    ' Enumerations
    '--------------
    Public Enum DDLCommand
        NONE
        CREATE_TABLE
        CREATE_GLOBAL_TEMPORARY_TABLE
        ALTER_TABLE
        COMMENT_ON_TABLE
        COMMENT_ON_COLUMN
    End Enum

    Private Enum TableGroup
        NONE
        SCHEMA_NAME
        TABLE_NAME
        COLUMN_LIST
    End Enum

    Private Enum ColumnGroup
        NONE
        COLUMN_NAME
        DATA_TYPE
        ARGUMENTS
        AUTO_DEFAULT
        DEFAULT_VALUE
    End Enum

    Private Enum TableColumnGroup
        NONE
        SCHEMA_NAME
        TABLE_NAME
        COMMENT
    End Enum

    Private Enum TableCommentGroup
        NONE
        SCHEMA_NAME
        TABLE_NAME
        COMMENT
    End Enum

    Private Enum ColumnCommentGroup
        NONE
        SCHEMA_NAME
        TABLE_NAME
        COLUMN_NAME
        COMMENT
    End Enum

    Private Enum NotNullGroup
        NONE
        SCHEMA_NAME
        TABLE_NAME
        COLUMN_NAME
        CONSTRAINT_NAME
    End Enum

    Private Enum PrimaryKeyGroup
        NONE
        SCHEMA_NAME
        TABLE_NAME
        CONSTRAINT_NAME
        COLUMN_LIST
    End Enum

    Private Enum UniqueGroup
        NONE
        SCHEMA_NAME
        TABLE_NAME
        CONSTRAINT_NAME
        COLUMN_LIST
    End Enum

    Private Enum ForeignGroup
        NONE
        SCHEMA_NAME
        TABLE_NAME
        CONSTRAINT_NAME
        COLUMN_LIST
        REF_SCHEMA_NAME
        REF_TABLE_NAME
        REF_COLUMN_LIST
    End Enum

    Private Enum CheckGroup
        NONE
        TABLE_NAME
        CONSTRAINT_NAME
        COLUMN_NAME
        CONDITION
    End Enum

    '-------------
    ' Constructor
    '-------------
    Public Sub New()
        Schemas = New List(Of Schema)
    End Sub

    '---------
    ' Methods
    '---------
    Public Sub ExecuteCommands()
        Dim ddlCommand As DDLCommand
        Dim progressValue As Integer
        Dim messageArgs As String()

        For Each schema As Schema In Schemas
            CurrentSchema = schema
            progressValue = 0

            messageArgs = New String() {schema.FileName, progressValue.ToString, schema.SQLCommands.Count.ToString}
            Call ShowStatus(EXECUTE_COMMANDS, progressValue, schema.SQLCommands.Count, messageArgs)

            For Each sqlCommand As String In schema.SQLCommands
                If CancelFlg Then Exit Sub

                progressValue += 1
                messageArgs = New String() {schema.FileName, progressValue.ToString, schema.SQLCommands.Count.ToString}
                Call ShowStatus(EXECUTE_COMMANDS, progressValue, messageArgs)

                ddlCommand = GetDDLCommand(sqlCommand)
                If Not ddlCommand = DDLCommand.NONE Then
                    Call CallByName(Me, GetMethodName(ddlCommand), CallType.Method, sqlCommand.Trim)
                End If

                Application.DoEvents()
            Next
        Next
    End Sub

    Public Sub CreateTable(ByVal command As String)
        Dim table As Table
        Dim columns As List(Of Column)
        Dim regexSuffix As String
        Dim commandRegex As String
        Dim schemaName As String
        Dim tableName As String
        Dim tableGroups As GroupCollection

        regexSuffix = "\s+" & SQLRegex.TABLE_NAME & "\s*" & SQLRegex.CREATE_COLUMN_LIST
        commandRegex = DDLCommand.CREATE_TABLE.ToRegex(String.Empty, regexSuffix)
        tableGroups = Regex.Match(command, commandRegex, RegexOptions.IgnoreCase).Groups

        schemaName = tableGroups.Item(TableGroup.SCHEMA_NAME).ToString
        tableName = tableGroups.Item(TableGroup.TABLE_NAME).ToString
        columns = GetColumns(tableGroups.Item(TableGroup.COLUMN_LIST).ToString.Trim)

        If IsNothing(CurrentSchema.Name) Then
            CurrentSchema.Name = schemaName
        End If

        table = New Table(CurrentSchema, tableName, columns)
        CurrentSchema.Tables.Add(table)
    End Sub

    Public Sub CreateGlobalTemporaryTable(ByVal command As String)
        Dim table As Table
        Dim columns As List(Of Column)
        Dim regexSuffix As String
        Dim commandRegex As String
        Dim schemaName As String
        Dim tableName As String
        Dim tableGroups As GroupCollection

        regexSuffix = "\s+" & SQLRegex.TABLE_NAME & "\s*" & SQLRegex.CREATE_COLUMN_LIST
        commandRegex = DDLCommand.CREATE_GLOBAL_TEMPORARY_TABLE.ToRegex(String.Empty, regexSuffix)
        tableGroups = Regex.Match(command, commandRegex).Groups

        schemaName = tableGroups.Item(TableGroup.SCHEMA_NAME).ToString
        tableName = tableGroups.Item(TableGroup.TABLE_NAME).ToString
        columns = GetColumns(tableGroups.Item(TableGroup.COLUMN_LIST).ToString.Trim)

        If IsNothing(CurrentSchema.Name) Then
            CurrentSchema.Name = schemaName
        End If

        table = New Table(CurrentSchema, tableName, columns)
        CurrentSchema.Tables.Add(table)
    End Sub

    Public Sub CommentOnTable(ByVal command As String)
        Dim regexSuffix As String
        Dim commandRegex As String
        Dim tableName As String
        Dim comment As String
        Dim commentGroups As GroupCollection

        regexSuffix = "\s+" & SQLRegex.TABLE_NAME & SQLRegex.COMMENT_ON
        commandRegex = DDLCommand.COMMENT_ON_TABLE.ToRegex(String.Empty, regexSuffix)
        commentGroups = Regex.Match(command, commandRegex, RegexOptions.IgnoreCase).Groups

        tableName = commentGroups.Item(TableCommentGroup.TABLE_NAME).ToString
        comment = commentGroups.Item(TableCommentGroup.COMMENT).ToString
        CurrentSchema.Table(tableName).Comment = comment
    End Sub

    Public Sub CommentOnColumn(ByVal command As String)
        Dim regexSuffix As String
        Dim commandRegex As String
        Dim tableName As String
        Dim columnName As String
        Dim comment As String
        Dim commentGroups As GroupCollection

        regexSuffix = "\s+" & SQLRegex.TABLE_NAME & "\." & SQLRegex.COLUMN_NAME & SQLRegex.COMMENT_ON
        commandRegex = DDLCommand.COMMENT_ON_COLUMN.ToRegex(String.Empty, regexSuffix)
        commentGroups = Regex.Match(command, commandRegex, RegexOptions.IgnoreCase).Groups

        tableName = commentGroups.Item(ColumnCommentGroup.TABLE_NAME).ToString
        columnName = commentGroups.Item(ColumnCommentGroup.COLUMN_NAME).ToString
        comment = commentGroups.Item(ColumnCommentGroup.COMMENT).ToString

        CurrentSchema.Table(tableName).Column(columnName).Comment = comment
    End Sub

    Public Sub AlterTable(ByVal command As String)
        For Each constraintType As Constraint._Type In [Enum].GetValues(GetType(Constraint._Type))
            If command.Contains(constraintType.EnumToString & Chr(32)) Then
                AddConstraint(constraintType, command)
            End If
        Next
    End Sub

    Private Sub AddConstraint(ByVal constraintType As Constraint._Type, ByVal command As String)
        Dim regexSuffix As String
        Dim commandRegex As String
        Dim methodName As String
        Dim constraintGroups As GroupCollection

        regexSuffix = "\s+" & SQLRegex.TABLE_NAME
        Select Case constraintType
            Case Constraint._Type.NOT_NULL
                regexSuffix &= "\s+MODIFY\s*\(" & SQLRegex.COLUMN_NAME
                regexSuffix &= "(?:\s+CONSTRAINT\s+" & SQLRegex.CONSTRAINT_NAME
                regexSuffix &= ")?\s+NOT\s+NULL\s+ENABLE\s*\)"

            Case Constraint._Type.PRIMARY_KEY
                regexSuffix &= SQLRegex.ALTER_ADD_CONSTRAINT & "\s+PRIMARY\s+KEY\s*"
                regexSuffix &= SQLRegex.ALTER_COLUMN_LIST & "\s*(?:.|\n)*ENABLE"

            Case Constraint._Type.UNIQUE
                regexSuffix &= SQLRegex.ALTER_ADD_CONSTRAINT & "\s+UNIQUE\s*"
                regexSuffix &= SQLRegex.ALTER_COLUMN_LIST & "\s*(?:.|\n)*ENABLE"

            Case Constraint._Type.FOREIGN_KEY
                regexSuffix &= SQLRegex.ALTER_ADD_CONSTRAINT & "\s+FOREIGN\s+KEY\s*"
                regexSuffix &= SQLRegex.ALTER_COLUMN_LIST
                regexSuffix &= "\s*REFERENCES\s+" & SQLRegex.TABLE_NAME & "\s*"
                regexSuffix &= SQLRegex.ALTER_COLUMN_LIST & "\s+ENABLE"

            Case Constraint._Type.CHECK
                regexSuffix &= SQLRegex.ALTER_ADD_CONSTRAINT & "\s+CHECK\s*\(\s*"
                regexSuffix &= SQLRegex.COLUMN_NAME & "((?:.|\n)+)\)"
        End Select

        methodName = GetMethodName(constraintType, "Add", "Constraint")
        commandRegex = DDLCommand.ALTER_TABLE.ToRegex(String.Empty, regexSuffix)
        constraintGroups = Regex.Match(command, commandRegex, RegexOptions.IgnoreCase).Groups

        Call CallByName(Me, methodName, CallType.Method, constraintGroups)
    End Sub

    Public Sub AddNotNullConstraint(ByVal constraintGroups As GroupCollection)
        Dim constraintName As String = constraintGroups.Item(NotNullGroup.CONSTRAINT_NAME).ToString
        Dim tableName As String = constraintGroups.Item(NotNullGroup.TABLE_NAME).ToString
        Dim columnName As String = constraintGroups.Item(NotNullGroup.COLUMN_NAME).ToString

        CurrentSchema.Table(tableName).Column(columnName).AddConstraint(constraintName, Constraint._Type.NOT_NULL)
    End Sub

    Public Sub AddPrimaryKeyConstraint(ByVal constraintGroups As GroupCollection)
        Dim constraintName As String = constraintGroups.Item(PrimaryKeyGroup.CONSTRAINT_NAME).ToString
        Dim tableName As String = constraintGroups.Item(PrimaryKeyGroup.TABLE_NAME).ToString
        Dim columnList As List(Of String) = GetElements(constraintGroups.Item(PrimaryKeyGroup.COLUMN_LIST).ToString)

        For Each columnName As String In columnList
            columnName = columnName.Replace(Chr(34), String.Empty)
            CurrentSchema.Table(tableName).Column(columnName).AddConstraint(constraintName, Constraint._Type.PRIMARY_KEY)
        Next
    End Sub

    Public Sub AddUniqueConstraint(ByVal constraintGroups As GroupCollection)
        Dim constraintName As String = constraintGroups.Item(UniqueGroup.CONSTRAINT_NAME).ToString
        Dim tableName As String = constraintGroups.Item(UniqueGroup.TABLE_NAME).ToString
        Dim columnList As List(Of String) = GetElements(constraintGroups.Item(UniqueGroup.COLUMN_LIST).ToString)

        For Each columnName As String In columnList
            columnName = columnName.Replace(Chr(34), String.Empty)
            CurrentSchema.Table(tableName).Column(columnName).AddConstraint(constraintName, Constraint._Type.UNIQUE)
        Next
    End Sub

    Public Sub AddForeignKeyConstraint(ByVal constraintGroups As GroupCollection)
        Dim constraintName As String = constraintGroups.Item(ForeignGroup.CONSTRAINT_NAME).ToString
        Dim tableName As String = constraintGroups.Item(ForeignGroup.TABLE_NAME).ToString
        Dim columnList As List(Of String) = GetElements(constraintGroups.Item(ForeignGroup.COLUMN_LIST).ToString)
        Dim refSchemaName As String = constraintGroups.Item(ForeignGroup.REF_SCHEMA_NAME).ToString
        Dim refTableName As String = constraintGroups.Item(ForeignGroup.REF_TABLE_NAME).ToString
        Dim refColumnList As List(Of String) = GetElements(constraintGroups.Item(ForeignGroup.REF_COLUMN_LIST).ToString)
        Dim refColumn As Column

        For index As Integer = 0 To columnList.Count - 1
            refColumn = Schema(refSchemaName).Table(refTableName).Column(refColumnList.Item(index))
            CurrentSchema.Table(tableName).Column(columnList.Item(index)).AddConstraint(constraintName, Constraint._Type.FOREIGN_KEY, refColumn)
        Next
    End Sub

    Public Sub AddCheckConstraint(ByVal constraintGroups As GroupCollection)
        Dim constraintName As String = constraintGroups.Item(CheckGroup.CONSTRAINT_NAME).ToString
        Dim tableName As String = constraintGroups.Item(CheckGroup.TABLE_NAME).ToString
        Dim columnName As String = constraintGroups.Item(CheckGroup.COLUMN_NAME).ToString
        Dim expression As String = constraintGroups.Item(CheckGroup.CONDITION).ToString

        CurrentSchema.Table(tableName).Column(columnName).AddConstraint(constraintName, Constraint._Type.CHECK, expression)
    End Sub

    '------------
    ' Functions
    '------------
    Private Function GetColumns(ByVal command As String) As List(Of Column)
        Dim columns As New List(Of Column)
        Dim columnList As List(Of String)
        Dim columnGroups As GroupCollection
        Dim column As Column
        Dim dataType As DataType
        Dim columnName As String
        Dim dataTypeString As String
        Dim dataTypeArgs As String
        Dim autoDefault As String
        Dim defaultValue As String

        columnList = GetElements(command)
        For Each columnString As String In columnList
            columnGroups = Regex.Match(columnString, SQLRegex.CREATE_COLUMN_SYNTAX).Groups

            columnName = columnGroups(ColumnGroup.COLUMN_NAME).ToString
            dataTypeString = columnGroups(ColumnGroup.DATA_TYPE).ToString
            dataTypeArgs = columnGroups(ColumnGroup.ARGUMENTS).ToString
            autoDefault = columnGroups(ColumnGroup.AUTO_DEFAULT).ToString

            If autoDefault.Contains("DEFAULT") Then
                defaultValue = columnGroups(ColumnGroup.DEFAULT_VALUE).ToString
            Else
                defaultValue = columnGroups(ColumnGroup.AUTO_DEFAULT).ToString
            End If

            dataType = GetDataType(dataTypeString, dataTypeArgs)
            column = New Column(columnName, dataType, defaultValue)
            columns.Add(column)
        Next

        Return columns
    End Function

    Private Function GetDataType(ByVal dataTypeString As String, Optional ByVal arguments As String = "") As DataType
        Dim dataType As DataType._Type

        dataTypeString = Chr(95) & dataTypeString.Replace(Chr(32), Chr(95))
        dataType = [Enum].Parse(GetType(DataType._Type), dataTypeString)

        Return New DataType(dataType, arguments)
    End Function

    Private Function GetDDLCommand(ByVal sqlCommand As String) As DDLCommand
        For Each command As DDLCommand In [Enum].GetValues(GetType(DDLCommand))
            If sqlCommand.Contains(command.EnumToString()) Then
                Return command
            End If
        Next

        Return DDLCommand.NONE
    End Function

    Private Function GetMethodName(ByVal value As [Enum], Optional ByVal prefix As String = "", Optional ByVal suffix As String = "") As String
        Dim methodName As String

        methodName = value.EnumToString()
        methodName = StrConv(methodName.ToLower, VbStrConv.ProperCase)
        methodName = methodName.Replace(Chr(32), String.Empty)
        methodName = prefix & methodName & suffix

        Return methodName
    End Function

    Public Function Schema(ByVal name As String) As Schema
        Return Schemas.Find(Function(findSchema) findSchema.Name = name)
    End Function
End Class
