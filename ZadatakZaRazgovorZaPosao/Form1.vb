Imports System.Data.SqlClient
Imports System.Net.Http
Imports Newtonsoft.Json
Imports System.Net.Mail
Imports System.Windows.Forms.VisualStyles.VisualStyleElement
Imports System.Net.Mail.MailAddress
Imports System.Text
Imports System.Net
Imports System.Text.RegularExpressions

Public Class Form1
    Dim connectionString As String = "Server=DESKTOP-L33G465;Database=Zadatak;Integrated Security=True;"
    Dim httpClient As New HttpClient()
    Dim userInfos As String
    Dim IsButtonEnabled As Boolean = True
    Private Async Sub Button1_ClickAsync(sender As Object, e As EventArgs) Handles SubmitButton.Click


        If IsButtonEnabled Then
            Try
                IsButtonEnabled = False
                SubmitButton.Enabled = False

                Dim name As String = NameTextBox.Text.Trim()
                Dim surname As String = SurNameTextBox.Text.Trim()
                Dim email As String = EmailTextBox.Text.Trim()

                Dim validationMessage As String = ValidateInput(name, surname, email)

                If validationMessage IsNot Nothing Then
                    MessageBox.Show(validationMessage, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    IsButtonEnabled = True
                    SubmitButton.Enabled = True
                    Return
                End If

                Try
                    userInfos = Await FetchUserDataAndInsertItIntoDatabase(NameTextBox.Text, SurNameTextBox.Text, EmailTextBox.Text)
                    DisplayUserInfo(userInfos)
                Catch ex As Exception
                    ShowErrorMessage(ex)
                End Try


                Try
                    SendEmail("test", userInfos, "testmail123465@fastmail.com")
                Catch ex As Exception
                    ShowErrorMessage(ex)
                End Try


                Await Task.Delay(TimeSpan.FromMinutes(1))

                IsButtonEnabled = True
                SubmitButton.Enabled = True
            Catch ex As Exception
                ShowErrorMessage(ex)
            End Try
        Else
            MessageBox.Show("Please wait for a minute before using the button again.")
        End If

    End Sub

    Private Function ValidateInput(name As String, surname As String, email As String) As String
        If String.IsNullOrWhiteSpace(name) Then
            Return "Please enter a name."
        End If

        If String.IsNullOrWhiteSpace(surname) Then
            Return "Please enter a surname."
        End If

        If String.IsNullOrWhiteSpace(email) Then
            Return "Please enter an email."
        ElseIf Not IsValidEmail(email) Then
            Return "Please enter a valid email address."
        End If

        For Each c As Char In name
            If Char.IsDigit(c) Then
                Return "Please do not use numbers in the name."
            End If
        Next

        For Each c As Char In surname
            If Char.IsDigit(c) Then
                Return "Please do not use numbers in the surname."
            End If
        Next

        Return Nothing
    End Function

    Private Function IsValidEmail(email As String) As Boolean
        Dim emailPattern As String = "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
        Dim regex As New Regex(emailPattern)
        Return regex.IsMatch(email)
    End Function


    Private Sub SendEmail(subject As String, body As String, recipientEmail As String)

        If String.IsNullOrWhiteSpace(body) Then
            MessageBox.Show("Email body is empty. Email not sent.")
            Return
        End If

        Try
            Dim senderEmail As String = "testmail123465@fastmail.com"
            Dim senderPassword As String = "b5ndtesy4ds2lm68"

            ' passzatestzazadatak
            Dim smtpClient As New SmtpClient("smtp.fastmail.com")
            smtpClient.Port = 587
            smtpClient.EnableSsl = True
            smtpClient.UseDefaultCredentials = False
            smtpClient.Credentials = New NetworkCredential(senderEmail, senderPassword)

            Dim mailMessage As New MailMessage(senderEmail, recipientEmail, subject, body)
            smtpClient.Send(mailMessage)

            MessageBox.Show("Email sent successfully!")
        Catch ex As Exception
            MessageBox.Show("An error occurred while sending the email: " & ex.Message)
        End Try
    End Sub

    Private Function GetHighestID(connection As SqlConnection, tableName As String, columnName As String) As Integer
        Dim query As String = $"SELECT ISNULL(MAX({columnName}), 0) FROM {tableName}"
        Using command As New SqlCommand(query, connection)
            Return Convert.ToInt32(command.ExecuteScalar())
        End Using
    End Function

    Private Function GetNextID(connection As SqlConnection, tableName As String, columnName As String) As Integer
        Dim highestID As Integer = GetHighestID(connection, tableName, tableName + "ID")
        Return highestID + 1
    End Function

    Private Sub InsertData(name As String, surname As String, email As String)

        Try
            Using connection As New SqlConnection(connectionString)
                connection.Open()
                Dim newClientID As Integer = GetNextID(connection, "Client", "ClientID")
                ExecuteInsertQuery(connection, newClientID, name, surname, email)
            End Using
        Catch ex As Exception
            Throw
        End Try
    End Sub

    Private Sub ExecuteInsertQuery(connection As SqlConnection, clientID As Integer, name As String, surname As String, email As String)

        Try
            Dim newClientID As Integer = GetNextID(connection, "Client", "ClientID")

            Dim insertQuery As String = "INSERT INTO Client (ClientID, name, surname, email) VALUES (@ClientID, @Name, @Surname, @Email)"
            Using command As New SqlCommand(insertQuery, connection)
                command.Parameters.AddWithValue("@ClientID", newClientID)
                command.Parameters.AddWithValue("@Name", name)
                command.Parameters.AddWithValue("@Surname", surname)
                command.Parameters.AddWithValue("@Email", email)
                command.ExecuteNonQuery()
            End Using

            MessageBox.Show("Data inserted successfully!")
        Catch ex As Exception
            MessageBox.Show("An error occurred: " & ex.Message)
        Finally
            connection.Close()
        End Try
    End Sub

    Private Function ValidateName(name As String) As Boolean
        Return System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-zA-ZčćžšđČĆŽŠĐ\s]+$")
    End Function


    Private Async Function FetchUserDataAndInsertItIntoDatabase(name As String, surname As String, email As String) As Task(Of String)
        Dim userInfos As String = ""
        
        Try
            Dim apiUrl As String = "https://jsonplaceholder.typicode.com/users"
            Using httpClient As New HttpClient()
                Dim response As HttpResponseMessage = Await httpClient.GetAsync(New Uri(apiUrl))

                If response.IsSuccessStatusCode Then
                    Dim jsonResponse As String = Await response.Content.ReadAsStringAsync()
                    Dim users As List(Of User) = JsonConvert.DeserializeObject(Of List(Of User))(jsonResponse)
                    userInfos = GetUserInfosAndInsertDataIntoDatabase(users, name, surname, email)
                Else
                    MessageBox.Show("Failed to fetch data from the API.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End Using
        Catch ex As Exception
            ShowErrorMessage(ex)
        End Try

        Return userInfos
    End Function

    Private Function GetUserInfosAndInsertDataIntoDatabase(users As List(Of User), name As String, surname As String, email As String) As String
        Dim fullName = name + " " + surname

        Dim userFound As Boolean = False

        Dim userInfoString As String =
                   $"Name: {name}" & vbCrLf &
                   $"Surname: {surname}" & vbCrLf &
                   $"Email: {email}" & vbCrLf

        For Each user As User In users
            If fullName = user.Name Or email = user.Email Then
                userFound = True
                Dim userInfoStringFull As String = $"Username: {user.Username}" & vbCrLf &
                                                $"Street: {user.Address.Street}" & vbCrLf &
                                                $"Suite: {user.Address.Suite}" & vbCrLf &
                                                $"City: {user.Address.City}" & vbCrLf &
                                                $"Zipcode: {user.Address.Zipcode}" & vbCrLf &
                                                $"Lat: {user.Address.Geo.Lat}" & vbCrLf &
                                                $"Lng: {user.Address.Geo.Lng}" & vbCrLf &
                                                $"Phone: {user.Phone}" & vbCrLf &
                                                $"Website: {user.Website}" & vbCrLf &
                                                $"Company Name: {user.Company.Name}" & vbCrLf &
                                                $"Company CatchPhrase: {user.Company.CatchPhrase}" & vbCrLf &
                                                $"Company BS: {user.Company.Bs}" & vbCrLf

                Dim combinedUserInfo As String = userInfoString & userInfoStringFull

                '                DisplayUserInfo(combinedUserInfo)
                InsertJSONData(user, name, surname, email)
                userInfoString = combinedUserInfo
                Exit For
            End If
        Next

        If Not userFound Then
            '        DisplayUserInfo(userInfoString)
            InsertData(name, surname, email)
        End If

        Return userInfoString
    End Function


    Private Sub DisplayUserInfo(userInfo As String)
        If Not String.IsNullOrEmpty(userInfo) Then
            MessageBox.Show(userInfo, "User Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub


    Private Sub InsertJSONData(user As User, name As String, surname As String, email As String)
        Try
            Using connection As New SqlConnection(connectionString)
                connection.Open()

                ExecuteInsertJSONQuery(connection, user, name, surname, email)
            End Using
        Catch ex As Exception
            Throw
        End Try
    End Sub

    Private Sub ExecuteInsertJSONQuery(connection As SqlConnection, user As User, name As String, surname As String, email As String)

        Dim companyID As Integer = GetNextID(connection, "Company", "CompanyID")
        Dim addressID As Integer = GetNextID(connection, "Adress", "AdressID")
        Dim geoID As Integer = GetNextID(connection, "Geo", "GeoID")
        Dim clientID As Integer = GetNextID(connection, "Client", "ClientID")

        Try
            Dim transaction As SqlTransaction = connection.BeginTransaction()

            Dim companyInsertQuery As String = "INSERT INTO Company (CompanyID, Name, CatchPhrase, Bs) VALUES (@CompanyID, @Name, @CatchPhrase, @Bs)"
            Using companyCommand As New SqlCommand(companyInsertQuery, connection, transaction)
                companyCommand.Parameters.AddWithValue("@CompanyID", companyID)
                companyCommand.Parameters.AddWithValue("@Name", user.Company.Name)
                companyCommand.Parameters.AddWithValue("@CatchPhrase", user.Company.CatchPhrase)
                companyCommand.Parameters.AddWithValue("@Bs", user.Company.Bs)
                companyCommand.ExecuteNonQuery()
            End Using

            Dim geoInsertQuery As String = "INSERT INTO Geo (GeoID, Lat, Lng) VALUES (@GeoID, @Lat, @Lng)"
            Using geoCommand As New SqlCommand(geoInsertQuery, connection, transaction)
                geoCommand.Parameters.AddWithValue("@GeoID", geoID)
                geoCommand.Parameters.AddWithValue("@Lat", user.Address.Geo.Lat)
                geoCommand.Parameters.AddWithValue("@Lng", user.Address.Geo.Lng)
                geoCommand.ExecuteNonQuery()
            End Using

            Dim addressInsertQuery As String = "INSERT INTO Adress (AdressID, Street, Suite, City, Zipcode, GeoID) VALUES (@AdressID, @Street, @Suite, @City, @Zipcode, @GeoID)"
            Using addressCommand As New SqlCommand(addressInsertQuery, connection, transaction)
                addressCommand.Parameters.AddWithValue("@AdressID", addressID)
                addressCommand.Parameters.AddWithValue("@Street", user.Address.Street)
                addressCommand.Parameters.AddWithValue("@Suite", user.Address.Suite)
                addressCommand.Parameters.AddWithValue("@City", user.Address.City)
                addressCommand.Parameters.AddWithValue("@Zipcode", user.Address.Zipcode)
                addressCommand.Parameters.AddWithValue("@GeoID", geoID)
                addressCommand.ExecuteNonQuery()
            End Using

            Dim clientInsertQuery As String = "INSERT INTO Client (ClientID, Username, AdressID, CompanyID, Name, Surname, Email ) VALUES (@ClientID, @Username, @AdressID, @CompanyID, @Name, @Surname, @Email )"
            Using clientCommand As New SqlCommand(clientInsertQuery, connection, transaction)
                clientCommand.Parameters.AddWithValue("@ClientID", clientID)
                clientCommand.Parameters.AddWithValue("@Username", user.Username)
                clientCommand.Parameters.AddWithValue("@AdressID", addressID)
                clientCommand.Parameters.AddWithValue("@CompanyID", companyID)
                clientCommand.Parameters.AddWithValue("@Name", name)
                clientCommand.Parameters.AddWithValue("@Surname", surname)
                clientCommand.Parameters.AddWithValue("@Email", email)
                clientCommand.ExecuteNonQuery()
            End Using

            transaction.Commit()

            MessageBox.Show("Data inserted successfully!")
        Catch ex As Exception
            MessageBox.Show("An error occurred: " & ex.Message)
        End Try
    End Sub




    Private Sub ShowErrorMessage(ex As Exception)
        MessageBox.Show("An error occurred: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub



End Class


Public Class Geo
    Public Property Lat As String
    Public Property Lng As String
End Class

Public Class Address
    Public Property Street As String
    Public Property Suite As String
    Public Property City As String
    Public Property Zipcode As String
    Public Property Geo As Geo
End Class

Public Class Company
    Public Property Name As String
    Public Property CatchPhrase As String
    Public Property Bs As String
End Class

Public Class User
    Public Property Id As Integer
    Public Property Name As String
    Public Property Username As String
    Public Property Email As String
    Public Property Address As Address
    Public Property Phone As String
    Public Property Website As String
    Public Property Company As Company
End Class

Public Class RootUser
    Public Property Users As List(Of User)
End Class


