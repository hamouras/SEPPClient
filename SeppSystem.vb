Imports System
Imports System.Runtime.InteropServices
Imports System.ComponentModel

Public NotInheritable Class Interaction
    Private Sub New()
    End Sub

    Friend Shared Sub WriteINIFile(pCat As String, pKey As String, pValue As String, pfile As String)
        NativeMethods.WritePrivateProfileString(pCat, pKey, pValue, pfile)
    End Sub

    Friend Shared Function readIniFile(pCat As String, pKey As String, pfile As String) As String
        Dim retValue As String = ""
        Dim strdata As New Text.StringBuilder(254)
        If NativeMethods.GetPrivateProfileString(pCat, pKey, "", strdata, strdata.Capacity, pfile) > 0 Then
            retValue = strdata.ToString
        End If
        Return retValue
    End Function

End Class

Friend NotInheritable Class NativeMethods

    Private Sub New()
    End Sub

    Friend Declare Auto Function WritePrivateProfileString Lib "Kernel32" (ByVal IpApplication As String,
                                                                        ByVal Ipkeyname As String,
                                                                        ByVal IpString As String,
                                                                        ByVal IpFileName As String) As Integer

    Friend Declare Auto Function GetPrivateProfileString Lib "kernel32" (ByVal lpAppName As String,
                                                                          ByVal lpKeyName As String,
                                                                          ByVal lpDefault As String,
                                                                          ByVal lpReturnedString As Text.StringBuilder,
                                                                          ByVal nSize As Integer,
                                                                          ByVal lpFileName As String) As Integer

End Class