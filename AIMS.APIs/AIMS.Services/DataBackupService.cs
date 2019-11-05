﻿using AIMS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO.Compression;
using System.Text;

namespace AIMS.Services
{
    public interface IDataBackupService
    {
        ActionResponse BackupData(string connString, string backupDir);
    }

    public class DataBackupService
    {
        public ActionResponse BackupData(string connString, string backupDir)
        {
            ActionResponse response = new ActionResponse();
            try
            {
                string[] saDatabases = new string[] { "AIMSDb" };
                using (SqlConnection sqlConnection = new SqlConnection(connString))
                {
                    sqlConnection.Open();

                    foreach (string dbName in saDatabases)
                    {
                        string backupFileNameWithoutExt = String.Format("{0}\\{1}_{2:yyyy-MM-dd_hh-mm-ss-tt}", backupDir, dbName, DateTime.Now.ToString());
                        string backupFileNameWithExt = String.Format("{0}.bak", backupFileNameWithoutExt);
                        string zipFileName = String.Format("{0}.zip", backupFileNameWithoutExt);

                        string cmdText = string.Format("BACKUP DATABASE {0}\r\nTO DISK = '{1}'", dbName, backupFileNameWithExt);

                        using (SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection))
                        {
                            sqlCommand.CommandTimeout = 0;
                            sqlCommand.ExecuteNonQuery();
                        }

                        ZipFile.CreateFromDirectory(backupFileNameWithExt, zipFileName);
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }
    }
}
