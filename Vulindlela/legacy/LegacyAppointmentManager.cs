// =============================================================================
//  NATIONAL CIVIC SERVICES AGENCY
//  Branch Counter System  -  "BranchDesk 2011"
//
//  File:      LegacyAppointmentManager.cs
//  Author:    (original author no longer with the Agency)
//  Modified:  2011, 2013, 2014, 2016, 2019, 2021, 2023
//
//  DO NOT EDIT WITHOUT SIGN-OFF FROM HEAD OFFICE.
//
//  NOTE FROM THE 2019 MAINTAINER:
//  "I was asked to add the SMS reminder. I could not find a better place to
//   put it so it is in the same method. Sorry. Also the branch code is
//   hardcoded because each branch gets its own build of this file."
//
//  This file is supplied to you as evidence. You are NOT required to fix it,
//  compile it, or run it. You are required to ANALYSE it.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net.Mail;

namespace Vulindlela.legacy
{
    public class LegacyAppointmentManager
    {
        // Every branch receives its own build of this file with these values
        // edited by hand before deployment.
        private string connectionString =
            "Server=BRANCHSRV01;Database=BranchDesk;User Id=sa;Password=Branch2011!;";

        private string smsGatewayKey = "SMSGW-7741-PROD-9930";
        private string branchCode = "JHB-CENTRAL-01";
        private string headOfficeExportPath = @"C:\BranchDesk\Export\";

        // -------------------------------------------------------------------
        // This one method books an appointment. It has grown since 2011.
        // -------------------------------------------------------------------
        public string BookAppointment(
            string idNumber,
            string fullName,
            string serviceType,
            string appointmentDate,
            string cellNumber,
            string documentFolder)
        {
            // ---- 1. Validation -------------------------------------------
            if (idNumber == null || idNumber.Length != 13)
            {
                return "ERROR: ID number must be 13 digits";
            }

            for (int i = 0; i < idNumber.Length; i++)
            {
                if (!char.IsDigit(idNumber[i]))
                {
                    return "ERROR: ID number must be numeric";
                }
            }

            if (fullName == null || fullName.Trim() == "")
            {
                return "ERROR: Name required";
            }

            // Service type decides the fee. Added to over the years.
            decimal fee = 0;
            if (serviceType == "ID_COLLECTION")
            {
                fee = 0;
            }
            else if (serviceType == "ID_APPLICATION")
            {
                fee = 140;
            }
            else if (serviceType == "PASSPORT")
            {
                fee = 400;
            }
            else if (serviceType == "PASSPORT_URGENT")
            {
                fee = 800;
            }
            else if (serviceType == "BIRTH_CERT")
            {
                fee = 75;
            }
            else if (serviceType == "MARRIAGE_CERT")
            {
                fee = 75;
            }
            else if (serviceType == "DEATH_CERT")
            {
                fee = 75;
            }
            else
            {
                return "ERROR: Unknown service type";
            }

            // 2016: a surcharge was added for two service types only.
            if (serviceType == "PASSPORT" || serviceType == "PASSPORT_URGENT")
            {
                fee = fee + 65;
            }

            // ---- 2. Check the slot is free -------------------------------
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            SqlCommand checkCmd = new SqlCommand(
                "SELECT COUNT(*) FROM Appointments WHERE BranchCode = '" + branchCode +
                "' AND AppointmentDate = '" + appointmentDate + "'", conn);

            int taken = (int)checkCmd.ExecuteScalar();

            if (taken >= 40)
            {
                return "ERROR: Branch full on that date";
            }

            // ---- 3. Write the appointment --------------------------------
            SqlCommand insertCmd = new SqlCommand(
                "INSERT INTO Appointments " +
                "(IdNumber, FullName, ServiceType, AppointmentDate, BranchCode, Fee) " +
                "VALUES ('" + idNumber + "','" + fullName + "','" + serviceType +
                "','" + appointmentDate + "','" + branchCode + "'," + fee + ")", conn);

            insertCmd.ExecuteNonQuery();

            // ---- 4. Check the citizen's documents ------------------------
            // Documents are scanned to a shared folder by the front desk.
            bool documentsOk = true;
            string missing = "";

            if (serviceType == "PASSPORT" || serviceType == "PASSPORT_URGENT")
            {
                if (!File.Exists(documentFolder + "\\" + idNumber + "_ID.pdf"))
                {
                    documentsOk = false;
                    missing = missing + "Certified ID copy; ";
                }
                if (!File.Exists(documentFolder + "\\" + idNumber + "_PHOTO.jpg"))
                {
                    documentsOk = false;
                    missing = missing + "Passport photograph; ";
                }
            }

            if (serviceType == "BIRTH_CERT")
            {
                if (!File.Exists(documentFolder + "\\" + idNumber + "_PARENT_ID.pdf"))
                {
                    documentsOk = false;
                    missing = missing + "Parent ID copy; ";
                }
            }

            // ---- 5. Send the SMS reminder (added 2019) -------------------
            try
            {
                SmtpClient smtp = new SmtpClient("smtp.branchdesk.local");
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("noreply@branchdesk.local");
                mail.To.Add(cellNumber + "@smsgateway.local");
                mail.Subject = "Appointment";
                mail.Body = "Dear " + fullName + ", your appointment at " + branchCode +
                            " is on " + appointmentDate + ". Fee: R" + fee +
                            ". Key: " + smsGatewayKey;
                smtp.Send(mail);
            }
            catch (Exception)
            {
                // If the SMS fails we carry on. The citizen will find out on the day.
            }

            // ---- 6. Write the head office export line (added 2013) -------
            // Head office reads this folder overnight. If the file is locked
            // the line is lost and head office never learns about this booking.
            try
            {
                StreamWriter writer = new StreamWriter(
                    headOfficeExportPath + "export_" + DateTime.Now.ToString("yyyyMMdd") + ".csv",
                    true);

                writer.WriteLine(idNumber + "," + fullName + "," + serviceType + "," +
                                 appointmentDate + "," + branchCode + "," + fee);
                writer.Close();
            }
            catch (Exception)
            {
                // Ignore. The overnight job will pick it up tomorrow. Usually.
            }

            // ---- 7. Print the counter slip (added 2014) ------------------
            Console.WriteLine("=====================================");
            Console.WriteLine(" NATIONAL CIVIC SERVICES AGENCY");
            Console.WriteLine(" BRANCH: " + branchCode);
            Console.WriteLine(" NAME:   " + fullName);
            Console.WriteLine(" ID:     " + idNumber);
            Console.WriteLine(" DATE:   " + appointmentDate);
            Console.WriteLine(" FEE:    R" + fee);
            Console.WriteLine("=====================================");

            if (documentsOk == false)
            {
                return "BOOKED BUT DOCUMENTS MISSING: " + missing;
            }

            return "BOOKED";
        }

        // -------------------------------------------------------------------
        // Used by the counter clerk to look up a citizen.
        // There is no record anywhere of who ran this or when.
        // -------------------------------------------------------------------
        public List<string> FindCitizen(string searchTerm)
        {
            List<string> results = new List<string>();

            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            SqlCommand cmd = new SqlCommand(
                "SELECT IdNumber, FullName, ServiceType, AppointmentDate FROM Appointments " +
                "WHERE FullName LIKE '%" + searchTerm + "%' OR IdNumber LIKE '%" + searchTerm + "%'",
                conn);

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                results.Add(reader[0] + " | " + reader[1] + " | " + reader[2] + " | " + reader[3]);
            }

            return results;
        }
    }
}
