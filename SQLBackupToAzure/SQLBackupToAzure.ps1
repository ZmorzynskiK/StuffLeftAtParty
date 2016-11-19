param([ValidateSet("full", "log")][string]$BackType = "full")

# CONFIGURE SOME SETTINGS ------>
$sqlBackupLocation = "c:\_SQLBACKUP\" # local path for backup files
$dbName = "<your_db_name>"
$backupFilesExtensions = "*.bak","*.trn"
$zipFileName = (Get-Date -Format yyyyMMdd_HHmmss) + ".zip"
$csvFileName = "log-info.csv"
$zipFilePath = $sqlBackupLocation + $zipFileName
$csvFilePath = $sqlBackupLocation + $csvFileName

$csvObjProps = @{ 'Date' = Get-Date -Format "yyyy-MM-dd HH:mm:ss"; 'Type' = $backType; 'Filename' = $zipFileName }
class EmailConfig
{
    [string]$To = "<log_receiver@email>"
    [string]$Subject = "[<your_app>] Backup log"
    [string]$From = "<sender@email>"
    [string]$Body = "SQL backup log is in the attachment."
    [string]$SMTPServer = "<sender_server>"
    [int]$SMTPPort = 587 # or 25
    [string]$SMTPPassword = "<sender_pass>"
}
$emailConfig = [EmailConfig]::new()

class AzureConfig
{
    [string]$AccountName = "<azure_account_name>"
    [string]$AccountKey = "<azure_account_key>"
    [string]$BackupContainer = "db-backup" # be sure to have this container created
}
$azureConfig = [AzureConfig]::new()

# END CONFIGURATION <---------

$startTime = Get-Date

function BackupSQL()
{
    $sqlBackupType = 'F'
    if( $BackType -eq "log")
    {
        $sqlBackupType = 'L'
    }

    sqlcmd -S .\SQLEXPRESS -E -Q "EXEC sp_BackupDatabases @backupLocation='$sqlBackupLocation', @databaseName='$dbName', @backupType='$sqlBackupType'"
}

function ListZipAndDeleteSQLFiles()
{
    # list files to be zipped
    $files = Get-ChildItem -Include $backupFilesExtensions -Path $sqlBackupLocation\*
    $files | Compress-Archive -DestinationPath $zipFilePath

    # delete src sql files
    $files | Remove-Item -Force
}

function UploadZipToCloud()
{
    try{
    $StorageConnectionString="DefaultEndpointsProtocol=https;AccountName=$($azureConfig.AccountName);AccountKey=$($azureConfig.AccountKey)"
    $Ctx = New-AzureStorageContext -ConnectionString $StorageConnectionString
    # upload
    Set-AzureStorageBlobContent -Context $Ctx -Container $azureConfig.BackupContainer -File $zipFilePath
    }
    catch{
       Write-Host "Uploading to cloud: $_" 
    }
}

function AddInfoToCSV()
{
    # create CSV object and write it to file
    $csvObjProps.FileSizeKB = (Get-ChildItem $zipFilePath).Length/1024
    $csvObjProps.TimeSpentSecs = (New-TimeSpan -End (Get-Date) -Start $startTime).TotalSeconds
    New-Object -TypeName PSObject -Property $csvObjProps | Export-Csv -Delimiter "," -Path $csvFilePath -NoTypeInformation -Encoding UTF8 -NoClobber -Append
}

function Send-EMail {
    Param ([Parameter(Mandatory=$true)][String]$EmailTo, [Parameter(Mandatory=$true)][String]$Subject, [Parameter(Mandatory=$true)] [String]$Body,
            [Parameter(Mandatory=$true)][String]$EmailFrom, [Parameter(Mandatory=$true)][String]$SMTPServer, [Parameter(Mandatory=$true)][int]$SMTPPort, 
            [Parameter(mandatory=$false)][String]$attachment,
             [Parameter(mandatory=$true)][String]$Password )

        $SMTPMessage = New-Object System.Net.Mail.MailMessage($EmailFrom,$EmailTo,$Subject,$Body)
        $SMTPattachment = $null
        if ($attachment -ne $null) {
            $SMTPattachment = New-Object System.Net.Mail.Attachment($attachment)
            $SMTPMessage.Attachments.Add($SMTPattachment)
        }
        $SMTPClient = New-Object Net.Mail.SmtpClient($SmtpServer, $SMTPPort) 
        $SMTPClient.EnableSsl = $true 
        $SMTPClient.Credentials = New-Object System.Net.NetworkCredential($EmailFrom, $Password); 
        $SMTPClient.Send($SMTPMessage)
        $SMTPClient.Dispose()
        if( $SMTPattachment -ne $null ){
            $SMTPattachment.Dispose()
            Remove-Variable -Name SMTPattachment
        }
        $SMTPMessage.Dispose()
        Remove-Variable -Name SMTPMessage
        Remove-Variable -Name SMTPClient
        Remove-Variable -Name Password

}

function SendEmailWithCSV(){
    try{
        Send-EMail -EmailTo $emailConfig.To -EmailFrom $emailConfig.From -Subject $emailConfig.Subject -Body $emailConfig.Body -SMTPServer $emailConfig.SMTPServer `
                    -SMTPPort $emailConfig.SMTPPort -attachment $csvFilePath -Password $emailConfig.SMTPPassword
    }
    catch{
       Write-Host "Error sending email: $_" 
       return
    }
    Remove-Item $csvFilePath -Force
}

Write-Host "Performing backup: $BackType"
# execute SQL backup first
BackupSQL
Write-Host "Backup created"
# create files to zip file
ListZipAndDeleteSQLFiles
Write-Host "Files zipped"
# upload to azure storage
UploadZipToCloud
Write-Host "Files uploaded to cloud"
# add to info csv
AddInfoToCSV
Write-Host "CSV info updated"
# send CSV to email if we are doing full backup
if( $BackType -eq "full" ){
    SendEmailWithCSV
    Write-Host "Email with CSV sent"
}

Write-Host "Done!"