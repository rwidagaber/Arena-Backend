using System;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connStr = "Server=db55749.public.databaseasp.net; Database=db55749; User Id=db55749; Password=4Wz?h%L2#N3q; Encrypt=False; MultipleActiveResultSets=True;";
        string email = "husseinymismmail@gmail.com";

        using (SqlConnection conn = new SqlConnection(connStr))
        {
            conn.Open();
            Console.WriteLine("Connected to Azure Database successfully.");

            // 1. Get User
            string userQuery = "SELECT Id, FirstName, LastName, Email, IsActive, IsDeleted, CreatedAt FROM AspNetUsers WHERE Email = @Email";
            Guid userId = Guid.Empty;
            using (SqlCommand cmd = new SqlCommand(userQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        userId = reader.GetGuid(0);
                        Console.WriteLine($"User: {reader.GetString(1)} {reader.GetString(2)}, Email: {reader.GetString(3)}, IsActive: {reader.GetBoolean(4)}, IsDeleted: {reader.GetBoolean(5)}, CreatedAt: {reader.GetDateTime(6)}");
                    }
                    else
                    {
                        Console.WriteLine("User not found.");
                        return;
                    }
                }
            }

            // 2. Get Member Profile
            Guid profileId = Guid.Empty;
            string profileQuery = "SELECT Id, CreatedAt FROM MemberProfiles WHERE UserId = @UserId";
            using (SqlCommand cmd = new SqlCommand(profileQuery, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        profileId = reader.GetGuid(0);
                        Console.WriteLine($"MemberProfile: {profileId}, CreatedAt: {reader.GetDateTime(1)}");
                    }
                    else
                    {
                        Console.WriteLine("Member profile not found.");
                    }
                }
            }

            if (profileId != Guid.Empty)
            {
                // 3. Get Subscriptions
                string subQuery = "SELECT Id, PlanId, Status, StartDate, EndDate, CreatedAt, CreatedBy FROM UserSubscriptions WHERE MemberProfileId = @ProfileId";
                using (SqlCommand cmd = new SqlCommand(subQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ProfileId", profileId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Guid subId = reader.GetGuid(0);
                            Guid planId = reader.GetGuid(1);
                            string status = reader.IsDBNull(2) ? "NULL" : reader.GetValue(2).ToString();
                            DateTime start = reader.GetDateTime(3);
                            DateTime end = reader.GetDateTime(4);
                            DateTime created = reader.GetDateTime(5);
                            string createdBy = reader.IsDBNull(6) ? "NULL" : reader.GetString(6);

                            Console.WriteLine($"Subscription: {subId}, PlanId: {planId}, Status: {status}, StartDate: {start}, EndDate: {end}, CreatedAt: {created}, CreatedBy: {createdBy}");

                            // 4. Get Payments for this subscription
                            string payQuery = "SELECT Id, Amount, PaymentMethod, TransactionId, Status, CreatedAt, CreatedBy FROM Payments WHERE UserSubscriptionId = @SubId";
                            using (SqlCommand cmdPay = new SqlCommand(payQuery, conn))
                            {
                                cmdPay.Parameters.AddWithValue("@SubId", subId);
                                using (SqlDataReader readerPay = cmdPay.ExecuteReader())
                                {
                                    while (readerPay.Read())
                                    {
                                        Guid payId = readerPay.GetGuid(0);
                                        decimal amount = readerPay.GetDecimal(1);
                                        string method = readerPay.IsDBNull(2) ? "NULL" : readerPay.GetValue(2).ToString();
                                        string txId = readerPay.IsDBNull(3) ? "NULL" : readerPay.GetString(3);
                                        string payStatus = readerPay.IsDBNull(4) ? "NULL" : readerPay.GetValue(4).ToString();
                                        DateTime payCreated = readerPay.GetDateTime(5);
                                        string payCreatedBy = readerPay.IsDBNull(6) ? "NULL" : readerPay.GetString(6);

                                        Console.WriteLine($"  -> Payment: {payId}, Amount: {amount}, Method: {method}, TxId: {txId}, Status: {payStatus}, CreatedAt: {payCreated}, CreatedBy: {payCreatedBy}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
