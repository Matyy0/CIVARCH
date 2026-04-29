using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Text.RegularExpressions;

namespace CIVARCH
{
    /// <summary>
    /// Handles server database
    /// </summary>
    public class DatabaseHandler()
    {
        /// <summary>
        /// Connection string načítán z appsettings.json → ConnectionStrings:DefaultConnection
        /// Hesla NESMÍ být v kódu – nastavte je v appsettings.json (lokálně) nebo v secrets.
        /// </summary>
        private static string CONNECTION_STRING =>
            new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Local.json", optional: true)
                .AddUserSecrets<DatabaseHandler>(optional: true)
                .AddEnvironmentVariables()
                .Build()
                .GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection není nastaveno v appsettings.json / appsettings.Local.json.");

        public static bool CheckRCExists(string rc)
        {
            string sql = @"SELECT OBCAN_RC
                    FROM tbOBCAN_NEW
                    WHERE OBCAN_RC = @rc

                    UNION

                    SELECT OBCAN_RC
                    FROM tbOBCAN
                    WHERE OBCAN_RC = @rc;
                    ";

            using (SqlConnection conn = new(CONNECTION_STRING))
            {
                conn.Open();
                using (SqlCommand cmd = new(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@rc", rc);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.HasRows;
                    }
                }
            }
        }

        /// <summary>
        /// Po kazde uprave zaznamu aktualizuje informace o rizeni.
        /// </summary>
        /// <param name="obcanRC"></param>
        public static void UpdateRizeniTable(string obcanRC)
        {
            /*
             1 najit nejstarsi zaznam
             2 nastavit valid na 0
             */

            string rizeni_id = "0";

            using (SqlConnection conn = new(CONNECTION_STRING))
            {
                conn.Open();
                using (SqlCommand cmd = new("SELECT TOP 1 RIZENI_Id FROM tbRIZENI WHERE RIZENI_RC = @rc AND RIZENI_Valid = 1 ORDER BY RIZENI_Id", conn))
                {
                    cmd.Parameters.AddWithValue("@rc", obcanRC);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rizeni_id = reader["RIZENI_Id"].ToString() ?? "0";
                        }
                    }
                }
            }

            using (SqlConnection conn = new(CONNECTION_STRING))
            {
                conn.Open();
                using (SqlCommand cmd = new("UPDATE tbRIZENI SET RIZENI_Valid = 0 WHERE RIZENI_Id = @id AND RIZENI_RC = @rc AND RIZENI_Valid = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@id", rizeni_id);
                    cmd.Parameters.AddWithValue("@rc", obcanRC);
                    cmd.ExecuteNonQuery();
                }
            }

            using (SqlConnection conn = new(CONNECTION_STRING))
            {
                conn.Open();
                using (SqlCommand cmd = new("UPDATE tbRIZENI_NEW SET RIZENI_Valid = 0 WHERE RIZENI_Id = @id AND RIZENI_RC = @rc AND RIZENI_Valid = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@id", rizeni_id);
                    cmd.Parameters.AddWithValue("@rc", obcanRC);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteZaznam(int id)
        {
            using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmdRizeni = new SqlCommand(
                            "DELETE FROM tbRIZENI_NEW WHERE RIZENI_RC = (SELECT OBCAN_RC FROM tbOBCAN_NEW WHERE OBCAN_Id = @id)",
                            conn, tx))
                        {
                            cmdRizeni.Parameters.AddWithValue("@id", id);
                            cmdRizeni.ExecuteNonQuery();
                        }

                        using (SqlCommand cmdObcan = new SqlCommand(
                            "DELETE FROM tbOBCAN_NEW WHERE OBCAN_Id = @id",
                            conn, tx))
                        {
                            cmdObcan.Parameters.AddWithValue("@id", id);
                            cmdObcan.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Metoda umoznujici ulozeni zaznamu do databaze. 
        /// Nejdrive nacte vsechny informace, ktere se ukladaji, a pote je nahraje na server.
        /// </summary>
        /// <param name="zaznam">Objekt zaznamu, ktery se ma ulozit</param>
        public static void SaveNewZaznam(Zaznam zaznam, int IDOverwrite = 0)
        {
            //starts indexing OBCAN_Id from highest id in tbOBCAN
            string obcan_prijmeni = zaznam.Obcan.Prijmeni;
            string obcan_jmeno = zaznam.Obcan.Jmeno;
            string obcan_titul = zaznam.Obcan.Titul;
            string obcan_rodneJm = zaznam.Obcan.RodneJm;
            string obcan_rc = zaznam.Obcan.RC;
            string obcan_orp = zaznam.Organizace;

            string obcan_adresa_ulice = "";
            string obcan_adresa_cp = "";
            string obcan_adresa_obec = "";
            string obcan_adresa_psc = "";

            if (zaznam.Obcan.Adresa != null)
            {
                obcan_adresa_ulice = zaznam.Obcan.Adresa.Ulice ?? "";
                obcan_adresa_cp = zaznam.Obcan.Adresa.CisloPopisne ?? "";
                obcan_adresa_obec = zaznam.Obcan.Adresa.Obec ?? "";
                obcan_adresa_psc = zaznam.Obcan.Adresa.Psc ?? "";
            }

            string rizeni_podnik = zaznam.Rizeni.Podnik;
            string rizeni_datNastupu1 = zaznam.Rizeni.DatNastupu1 + " 0:00:00";
            string rizeni_datNastupu2 = zaznam.Rizeni.DatNastupu2 + " 0:00:00";
            string rizeni_datum = zaznam.Rizeni.Datum + " 0:00:00";
            string rizeni_datVystaveno = zaznam.Rizeni.DatVystaveno + " 0:00:00";
            string rizeni_stav = zaznam.Rizeni.Stav;
            //Gets highest obcan_Id in obcan_new
            var highestObcanID = GetHighestColumnIndexAsync("tbOBCAN", "OBCAN_Id").GetAwaiter().GetResult();
            int id = GetHighestColumnIndexAsync("tbOBCAN_NEW", "OBCAN_Id", highestObcanID).GetAwaiter().GetResult();
            if (IDOverwrite == 0)
            {
                if (id == -1) { id = ++highestObcanID; }
                else { id++; }
            }
            else { id = IDOverwrite; }

            try
            {
                using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
                {
                    conn.Open();
                    string sqlObcan = @"
                        SET IDENTITY_INSERT tbOBCAN_NEW ON
                        INSERT INTO tbOBCAN_NEW (
                            OBCAN_ID, OBCAN_RC, OBCAN_Jmeno, OBCAN_Prijmeni, OBCAN_Titul, 
                            OBCAN_RodneJm, OBCAN_AdrUlice, OBCAN_AdrCP, 
                            OBCAN_AdrObec, OBCAN_AdrPSC, OBCAN_CisORP
                        ) VALUES (
                            @id, @rc, @jmeno, @prijmeni, @titul, @rodneJm, @ulice, @cp, @obec, @psc, @orp
                        )
                        SET IDENTITY_INSERT tbOBCAN_NEW OFF";

                    string sqlRizeni = @"
                        INSERT INTO tbRIZENI_NEW (
                            RIZENI_RC, RIZENI_Podnik, RIZENI_DatNastupu1, 
                            RIZENI_DatNastupu2, RIZENI_Datum, RIZENI_DatVystaveno, 
                            RIZENI_Stav, RIZENI_Valid
                        ) VALUES (
                            @rc, @podnik, @datNastupu1, @datNastupu2, @datum, @datVystaveno, @stav, 1
                        )";

                    using (SqlCommand cmdObcan = new SqlCommand(sqlObcan, conn))
                    {
                        cmdObcan.Parameters.AddWithValue("@id", id);
                        cmdObcan.Parameters.AddWithValue("@rc", obcan_rc);
                        cmdObcan.Parameters.AddWithValue("@jmeno", obcan_jmeno);
                        cmdObcan.Parameters.AddWithValue("@prijmeni", obcan_prijmeni);
                        cmdObcan.Parameters.AddWithValue("@titul", obcan_titul);
                        cmdObcan.Parameters.AddWithValue("@rodneJm", obcan_rodneJm);
                        cmdObcan.Parameters.AddWithValue("@ulice", obcan_adresa_ulice);
                        cmdObcan.Parameters.AddWithValue("@cp", obcan_adresa_cp);
                        cmdObcan.Parameters.AddWithValue("@obec", obcan_adresa_obec);
                        cmdObcan.Parameters.AddWithValue("@psc", obcan_adresa_psc);
                        cmdObcan.Parameters.AddWithValue("@orp", obcan_orp);

                        cmdObcan.ExecuteNonQuery();
                    }

                    using (SqlCommand cmdRizeni = new SqlCommand(sqlRizeni, conn))
                    {
                        cmdRizeni.Parameters.AddWithValue("@rc", obcan_rc);
                        cmdRizeni.Parameters.AddWithValue("@podnik", rizeni_podnik);
                        cmdRizeni.Parameters.AddWithValue("@datNastupu1", rizeni_datNastupu1);
                        cmdRizeni.Parameters.AddWithValue("@datNastupu2", rizeni_datNastupu2);
                        cmdRizeni.Parameters.AddWithValue("@datum", rizeni_datum);
                        cmdRizeni.Parameters.AddWithValue("@datVystaveno", rizeni_datVystaveno);
                        cmdRizeni.Parameters.AddWithValue("@stav", rizeni_stav);

                        cmdRizeni.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database error: " + ex.Message);
            }
        }

        public static async Task<string> GetCisoOrp(string podnik)
        {
            using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
            {
                string getRcQuery = @"SELECT TOP(1) PODNIK_CisORP FROM tbPODNIK WHERE PODNIK_OrgNazev1 = @podnik";
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(getRcQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@podnik", podnik);

                    return cmd.ExecuteScalar().ToString();
                }
            }
        }

        public static void UpdateZaznam(Zaznam zaznam)
        {
            //indexes based on if its in old or new
            string old_id = zaznam.Obcan.Id.ToString();
            string obcan_prijmeni = zaznam.Obcan.Prijmeni;
            string obcan_jmeno = zaznam.Obcan.Jmeno;
            string obcan_titul = zaznam.Obcan.Titul;
            string obcan_rodneJm = zaznam.Obcan.RodneJm;
            string obcan_rc = zaznam.Obcan.RC;
            string obcan_orp = zaznam.Obcan.OrganizaceCislo;

            string obcan_adresa_ulice = "";
            string obcan_adresa_cp = "";
            string obcan_adresa_obec = "";
            string obcan_adresa_psc = "";

            if (zaznam.Obcan.Adresa != null)
            {
                obcan_adresa_ulice = zaznam.Obcan.Adresa.Ulice ?? "";
                obcan_adresa_cp = zaznam.Obcan.Adresa.CisloPopisne ?? "";
                obcan_adresa_obec = zaznam.Obcan.Adresa.Obec ?? "";
                obcan_adresa_psc = zaznam.Obcan.Adresa.Psc ?? "";
            }

            string rizeni_podnik = zaznam.Rizeni.Podnik;
            string rizeni_datNastupu1 = zaznam.Rizeni.DatNastupu1 + " 0:00:00";
            string rizeni_datNastupu2 = zaznam.Rizeni.DatNastupu2 + " 0:00:00";
            string rizeni_datum = zaznam.Rizeni.Datum + " 0:00:00";
            string rizeni_datVystaveno = zaznam.Rizeni.DatVystaveno + " 0:00:00";
            string rizeni_stav = zaznam.Rizeni.Stav;

            try
            {
                using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
                {
                    string getRcQuery = @"SELECT
                            CASE 
                                WHEN EXISTS (
                                    SELECT 1
                                    FROM tbOBCAN_NEW
                                    WHERE OBCAN_Id = @id
                                )
                                THEN 1
                                ELSE 0
                            END AS ExistsInNew;
                        ";
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(getRcQuery, conn))
                    {
                        var id = Convert.ToInt32(old_id);
                        cmd.Parameters.AddWithValue("@id", id);

                        var result = cmd.ExecuteScalar();
                        if (result.ToString().IsNullOrEmpty() || result.ToString() == "0")
                        {
                            SaveNewZaznam(zaznam, Convert.ToInt32(old_id));
                        }
                    }
                    string sqlRizeni = @"UPDATE tbRIZENI_NEW
                SET 
                    RIZENI_Podnik = @podnik,
                    RIZENI_DatNastupu1 = @datNastupu1,
                    RIZENI_DatNastupu2 = @datNastupu2,
                    RIZENI_Datum = @datum,
                    RIZENI_DatVystaveno = @datVystaveno,
                    RIZENI_Stav = @stav,
                    RIZENI_Valid = 1,
                    RIZENI_RC = @rc
                WHERE RIZENI_RC = (
                SELECT OBCAN_RC FROM tbOBCAN_NEW WHERE Obcan_Id = @id
            );";

                    string sqlObcan = @"
                UPDATE tbOBCAN_NEW
                    SET 
                        OBCAN_RC = @rc,
                        OBCAN_Jmeno = @jmeno,
                        OBCAN_Prijmeni = @prijmeni,
                        OBCAN_Titul = @titul,
                        OBCAN_RodneJm = @rodneJm,
                        OBCAN_AdrUlice = @ulice,
                        OBCAN_AdrCP = @cp,
                        OBCAN_AdrObec = @obec,
                        OBCAN_AdrPSC = @psc,
                        OBCAN_CisORP = @orp
                    WHERE Obcan_Id = @id;";

                    using (SqlCommand cmdRizeni = new SqlCommand(sqlRizeni, conn))
                    {
                        cmdRizeni.Parameters.AddWithValue("@id", old_id);
                        cmdRizeni.Parameters.AddWithValue("@rc", obcan_rc);
                        cmdRizeni.Parameters.AddWithValue("@podnik", rizeni_podnik);
                        cmdRizeni.Parameters.AddWithValue("@datNastupu1", rizeni_datNastupu1);
                        cmdRizeni.Parameters.AddWithValue("@datNastupu2", rizeni_datNastupu2);
                        cmdRizeni.Parameters.AddWithValue("@datum", rizeni_datum);
                        cmdRizeni.Parameters.AddWithValue("@datVystaveno", rizeni_datVystaveno);
                        cmdRizeni.Parameters.AddWithValue("@stav", rizeni_stav);

                        cmdRizeni.ExecuteNonQuery();
                    }

                    using (SqlCommand cmdObcan = new SqlCommand(sqlObcan, conn))
                    {
                        cmdObcan.Parameters.AddWithValue("@id", old_id);
                        cmdObcan.Parameters.AddWithValue("@rc", obcan_rc);
                        cmdObcan.Parameters.AddWithValue("@jmeno", obcan_jmeno);
                        cmdObcan.Parameters.AddWithValue("@prijmeni", obcan_prijmeni);
                        cmdObcan.Parameters.AddWithValue("@titul", obcan_titul);
                        cmdObcan.Parameters.AddWithValue("@rodneJm", obcan_rodneJm);
                        cmdObcan.Parameters.AddWithValue("@ulice", obcan_adresa_ulice);
                        cmdObcan.Parameters.AddWithValue("@cp", obcan_adresa_cp);
                        cmdObcan.Parameters.AddWithValue("@obec", obcan_adresa_obec);
                        cmdObcan.Parameters.AddWithValue("@psc", obcan_adresa_psc);
                        cmdObcan.Parameters.AddWithValue("@orp", obcan_orp);

                        cmdObcan.ExecuteNonQuery();
                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database error: " + ex.Message);
            }
        }

        public static void SavePodnik(Podnik podnik)
        {
            using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
            {
                conn.Open();
                string sqlObcan = @"
                        INSERT INTO tbPODNIK (
                            PODNIK_Id, PODNIK_OkrOrp, PODNIK_CisOrp, PODNIK_OrgNazev1, PODNIK_OrgUlice, 
                            PODNIK_OrgObec, PODNIK_OrgPSC, PODNIK_PredmCinn
                        ) VALUES (
                            @Id, @OkrOrp, @CisOrp, @OrgNazev1, @Ulice, @Obec, @Psc, @PredmetCinn
                        )";

                using (SqlCommand cmdObcan = new SqlCommand(sqlObcan, conn))
                {
                    cmdObcan.Parameters.AddWithValue("@Id", podnik.Id);
                    cmdObcan.Parameters.AddWithValue("@OkrOrp", podnik.OkrOrp);
                    cmdObcan.Parameters.AddWithValue("@CisOrp", podnik.CisOrp);
                    cmdObcan.Parameters.AddWithValue("@OrgNazev1", podnik.Nazev1);
                    cmdObcan.Parameters.AddWithValue("@Ulice", podnik.AdresaUlice);
                    cmdObcan.Parameters.AddWithValue("@Obec", podnik.AdresaObec);
                    cmdObcan.Parameters.AddWithValue("@Psc", podnik.AdresaPSC);
                    cmdObcan.Parameters.AddWithValue("@PredmetCinn", podnik.PredmetCinn);

                    cmdObcan.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Nacte pocet vsech obcanu, kteri splnuji podminku parametru (query).
        /// </summary>
        /// <param name="query">Vyhledavaci parametr</param>
        /// <returns>Vraci pocet obcanu, kteri splnuji dany vyhledavaci parametr</returns>
        public static async Task<int> LoadObcaneCount(string query = "")
        {
            int pocet = 0; // pocet zaznamu
            string sql;
            var parameters = new List<SqlParameter>();

            // kontrola jestli se ma vyhledavat
            if (query == "" || query == null)
            {
                // vsechny zaznamy
                sql = " select " +
                      "     __OBCAN_COUNT = count(OBCAN_Id)" +
                      " from tbOBCAN ";
            }
            else
            {
                // je potreba vyhledavat
                int queryLength = query.Split(' ').Length;

                if (queryLength == 1)
                {
                    sql = @"
                        SELECT COUNT(OBCAN_Id) AS __OBCAN_COUNT
                        FROM tbOBCAN
                        WHERE
                            OBCAN_Jmeno    COLLATE Czech_CI_AI LIKE @p0 OR
                            OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p0 OR
                            OBCAN_RC LIKE @p0 OR
                            OBCAN_Id LIKE @p0 OR
                            OBCAN_Adr1Obec COLLATE Czech_CI_AI LIKE @p0";

                    parameters.Add(new SqlParameter("@p0", $"%{query}%"));
                }
                else
                {
                    sql = "SELECT COUNT(OBCAN_Id) AS __OBCAN_COUNT FROM tbOBCAN WHERE ";

                    string[] qs = query.Split(' ');

                    if (queryLength == 2)
                    {
                        sql += @"
                            (OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p0 AND OBCAN_Jmeno COLLATE Czech_CI_AI LIKE @p1) OR
                            (OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p1 AND OBCAN_Jmeno COLLATE Czech_CI_AI LIKE @p0) OR
                            (OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p0 AND OBCAN_RC LIKE @p1) OR
                            (OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p1 AND OBCAN_RC LIKE @p0) OR
                            (OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p0 AND OBCAN_Adr1Obec COLLATE Czech_CI_AI LIKE @p1) OR
                            (OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p1 AND OBCAN_Adr1Obec COLLATE Czech_CI_AI LIKE @p0) OR
                            (OBCAN_Adr1Obec COLLATE Czech_CI_AI LIKE @p2)";

                        parameters.Add(new SqlParameter("@p0", $"%{qs[0]}%"));
                        parameters.Add(new SqlParameter("@p1", $"%{qs[1]}%"));
                        parameters.Add(new SqlParameter("@p2", $"%{qs[0]} {qs[1]}%"));
                    }
                    else if (queryLength == 3)
                    {
                        sql += @"
                            (OBCAN_Jmeno COLLATE Czech_CI_AI LIKE @p0 AND OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p1 AND OBCAN_RC LIKE @p2) OR
                            (OBCAN_Jmeno COLLATE Czech_CI_AI LIKE @p1 AND OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p2 AND OBCAN_RC LIKE @p0) OR
                            (OBCAN_Jmeno COLLATE Czech_CI_AI LIKE @p2 AND OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p0 AND OBCAN_RC LIKE @p1) OR
                            (OBCAN_Jmeno COLLATE Czech_CI_AI LIKE @p2 AND OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p1 AND OBCAN_RC LIKE @p0) OR
                            (OBCAN_Jmeno COLLATE Czech_CI_AI LIKE @p0 AND OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p2 AND OBCAN_RC LIKE @p1) OR
                            (OBCAN_Jmeno COLLATE Czech_CI_AI LIKE @p1 AND OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @p0 AND OBCAN_RC LIKE @p2) OR
                            (OBCAN_Adr1Obec COLLATE Czech_CI_AI LIKE @p3)";

                        parameters.Add(new SqlParameter("@p0", $"%{qs[0]}%"));
                        parameters.Add(new SqlParameter("@p1", $"%{qs[1]}%"));
                        parameters.Add(new SqlParameter("@p2", $"%{qs[2]}%"));
                        parameters.Add(new SqlParameter("@p3", $"%{qs[0]} {qs[1]} {qs[2]}%"));
                    }
                    else
                    {
                        throw new NotSupportedException("Query lengths greater than 3 are not supported.");
                    }
                }
            }

            int count = 0;

            using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(param);
                    }

                    object result = await cmd.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int parsedCount))
                    {
                        count = parsedCount;
                    }
                }
            }

            return count;
        }

        public static async Task<List<string>> LoadPodnikNames()
        {
            var names = new List<string>();

            string sql = @"SELECT [PODNIK_OrgNazev1]
                   FROM tbPODNIK
                   WHERE [PODNIK_OrgNazev1] IS NOT NULL";

            using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            string rawName = reader.GetString(0).Trim();

                            if (!string.IsNullOrWhiteSpace(rawName))
                            {
                                names.Add(rawName);
                            }
                        }
                    }
                }
            }

            // Remove duplicates after trim — case-insensitive optional
            var distinctNames = names
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return distinctNames;
        }



        /// <summary>
        /// Nacte vsechny obcany s ID v rozmezi startIndex a endIndex a pripadne pouze ty, kteri splnuji vyhledavaci parametr.
        /// </summary>
        /// <param name="startIndex">Pocatecni ID</param>
        /// <param name="endIndex">Konecne ID</param>
        /// <param name="query">Vyhledavaci parametr</param>
        /// <returns>Vraci seznam obcanu s ID v rozmezi startIndex a endIndex a kteri splnuji dany vyhledavaci parametr</returns>
        public static async Task<List<Obcan>> LoadObcaneListQuery(int startIndex = 1, int endIndex = 51, string query = "", string orderby = "OBCAN_Jmeno")
        {
            List<Obcan> _obcane = new();

            string sql;
            List<SqlParameter> parameters = new();

            if (string.IsNullOrEmpty(query))
            {
                sql = $@"
                        WITH Combined AS (
                            SELECT 
                                OBCAN_RC,
                                OBCAN_Jmeno,
                                OBCAN_Prijmeni,
                                OBCAN_Titul,
                                OBCAN_RodneJm,
                                OBCAN_AdrUlice AS OBCAN_Adr1Ulice,
                                OBCAN_AdrCP AS OBCAN_Adr1CP,
                                OBCAN_AdrObec AS OBCAN_Adr1Obec,
                                OBCAN_AdrPSC AS OBCAN_Adr1PSC,
                                OBCAN_CisORP,
                                OBCAN_ID
                            FROM tbOBCAN_NEW

                            UNION ALL

                            SELECT 
                                o.OBCAN_RC,
                                o.OBCAN_Jmeno,
                                o.OBCAN_Prijmeni,
                                o.OBCAN_Titul,
                                o.OBCAN_RodneJm,
                                o.OBCAN_Adr1Ulice,
                                o.OBCAN_Adr1CP,
                                o.OBCAN_Adr1Obec,
                                o.OBCAN_Adr1PSC,
                                o.OBCAN_CisORP,
                                o.OBCAN_ID
                            FROM tbOBCAN o
                        ),
                        Ordered AS (
                            SELECT *, ROW_NUMBER() OVER (ORDER BY {orderby}) AS rn
                            FROM Combined
                        )
                        SELECT *
                        FROM Ordered
                        WHERE rn BETWEEN @startIndex AND @endIndex;

                    ";

                parameters.Add(new SqlParameter("@startIndex", startIndex));
                parameters.Add(new SqlParameter("@endIndex", endIndex));
                parameters.Add(new SqlParameter("@orderby", orderby));
            }
            else
            {
                query = query.Trim();
                int queryLength = query.Split(' ').Length;

                if (queryLength == 1)
                {
                    if (query.Split('.').Length == 3)
                    {
                        var parts = query.Split('.');
                        query = parts[2].Substring(2, 2) + parts[1] + parts[0];
                    }

                    sql = $@"
                    SELECT TOP (@top) *
                    FROM (
                        SELECT 
                            OBCAN_Prijmeni,
                            OBCAN_Jmeno,
                            OBCAN_Titul,
                            OBCAN_RC,
                            OBCAN_RodneJm,
                            OBCAN_AdrUlice AS OBCAN_Adr1Ulice,
                            OBCAN_AdrCP AS OBCAN_Adr1Cp,
                            OBCAN_AdrObec AS OBCAN_Adr1Obec,
                            OBCAN_AdrPSC AS OBCAN_Adr1PSC,
                            OBCAN_CisORP,
                            OBCAN_ID
                        FROM tbOBCAN_NEW

                        UNION ALL

                        SELECT 
                            OBCAN_Prijmeni,
                            OBCAN_Jmeno,
                            OBCAN_Titul,
                            OBCAN_RC,
                            OBCAN_RodneJm,
                            OBCAN_Adr1Ulice,
                            OBCAN_Adr1Cp,
                            OBCAN_Adr1Obec,
                            OBCAN_Adr1PSC,
                            OBCAN_CisORP,
                            OBCAN_Id
                        FROM tbOBCAN o
                        WHERE NOT EXISTS (
                            SELECT 1 FROM tbOBCAN_NEW n WHERE n.OBCAN_RC = o.OBCAN_RC
                        )
                    ) Combined
                    WHERE
                        OBCAN_Jmeno    COLLATE Czech_CI_AI LIKE @search OR
                        OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE @search OR
                        OBCAN_RC LIKE @search OR
                        CAST(OBCAN_Id AS NVARCHAR) LIKE @search
                    ORDER BY {orderby};

                    ";

                    parameters.Add(new SqlParameter("@top", endIndex));
                    parameters.Add(new SqlParameter("@search", $"%{query}%"));
                    parameters.Add(new SqlParameter("@orderby", orderby));
                }
                else
                {

                    sql = @"
                        SELECT TOP (@top) *
                        FROM (
                            SELECT 
                                OBCAN_Prijmeni, OBCAN_Jmeno, OBCAN_Titul, OBCAN_RC, OBCAN_RodneJm,
                                OBCAN_AdrUlice AS OBCAN_Adr1Ulice,
                                OBCAN_AdrCP AS OBCAN_Adr1Cp,
                                OBCAN_AdrObec AS OBCAN_Adr1Obec,
                                OBCAN_AdrPSC AS OBCAN_Adr1PSC,
                                OBCAN_CisORP, OBCAN_ID
                        FROM tbOBCAN_NEW

                            UNION ALL

                            SELECT 
                                OBCAN_Prijmeni, OBCAN_Jmeno, OBCAN_Titul, OBCAN_RC, OBCAN_RodneJm,
                                OBCAN_Adr1Ulice, OBCAN_Adr1Cp, OBCAN_Adr1Obec, OBCAN_Adr1PSC,
                                OBCAN_CisORP, OBCAN_Id
                            FROM tbOBCAN o
                            WHERE NOT EXISTS (
                                SELECT 1 FROM tbOBCAN_NEW n WHERE n.OBCAN_RC = o.OBCAN_RC
                            )
                        ) Combined
                        WHERE
                        ";

                    parameters.Add(new SqlParameter("@top", endIndex));

                    var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var conditions = new List<string>();
                    for (int i = 0; i < words.Length; i++)
                    {
                        string p = $"@p{i}";
                        conditions.Add($"(OBCAN_Jmeno COLLATE Czech_CI_AI LIKE {p} OR OBCAN_Prijmeni COLLATE Czech_CI_AI LIKE {p} OR OBCAN_RC LIKE {p} OR OBCAN_Adr1Obec COLLATE Czech_CI_AI LIKE {p})");
                        parameters.Add(new SqlParameter(p, $"%{words[i]}%"));
                    }

                    sql += string.Join(" AND ", conditions);
                }
            }

            using SqlConnection conn = new(CONNECTION_STRING);
            await conn.OpenAsync();

            using SqlCommand cmd = new(sql, conn);

            foreach (var param in parameters)
            {
                cmd.Parameters.Add(param);
            }

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string[] data = ReaderToStringArray(reader, "",
                    "OBCAN_Prijmeni", "OBCAN_Jmeno", "OBCAN_Titul", "OBCAN_RC", "OBCAN_RodneJm",
                    "OBCAN_Adr1Ulice", "OBCAN_Adr1Cp", "OBCAN_Adr1Obec", "OBCAN_Adr1PSC",
                    "OBCAN_CisORP", "OBCAN_Id");

                if (data.Length < 11)
                {
                    throw new Exception("Chyba: Očekávalo se 11 hodnot, ale načteno bylo méně.");
                }

                string rodneCislo = string.IsNullOrEmpty(data[3]) ? "Neznámé" : data[3];

                string datumNarozeni;
                try
                {
                    datumNarozeni = DataHandler.ConvertRCToDatumNarozeni(rodneCislo);
                }
                catch
                {
                    datumNarozeni = "Neznámé datum"; // If RC invalid, fallback
                }

                Obcan _obcan = new(data[0], data[1], data[2], rodneCislo, data[4],
                                   new(data[5], data[6], data[7], data[8]),
                                   data[9], Convert.ToInt32(data[10]));

                _obcane.Add(_obcan);
            }

            return _obcane;
        }

        public static async Task<int> GetHighestColumnIndexAsync(string table, string column, int min = int.MinValue, int max = int.MaxValue)
        {
            string sql = $@"
                SELECT MAX(CAST([{column}] AS INT)) AS MaxValue 
                FROM [{table}]
                WHERE CAST([{column}] AS INT) BETWEEN @Min AND @Max";

            using SqlConnection conn = new(CONNECTION_STRING);
            await conn.OpenAsync();

            using SqlCommand cmd = new(sql, conn);
            cmd.Parameters.AddWithValue("@Min", min);
            cmd.Parameters.AddWithValue("@Max", max);

            object result = await cmd.ExecuteScalarAsync();

            if (result != DBNull.Value && result != null)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                return -1;
            }
        }



        /// <summary>
        /// Nacte jeden zaznam podle ID obcana
        /// </summary>
        /// <param name="id">ID obcana, jehoz zaznam ma byt nacten</param>
        /// <returns>Vraci jeden zaznam</returns>
        public static async Task<Zaznam> LoadSingleZaznamById(string id)
        {
            string sqlObcan = "";
            sqlObcan = @"
SELECT
    OBCAN_Id,
    OBCAN_Prijmeni,
    OBCAN_Jmeno,
    OBCAN_RC,
    OBCAN_Titul,
    OBCAN_RodneJm,
    OBCAN_CisORP,
    OBCAN_AdrUlice AS OBCAN_Adr1Ulice,
    OBCAN_AdrPSC AS OBCAN_Adr1PSC,
    OBCAN_AdrCp AS OBCAN_Adr1Cp,
    OBCAN_UsrZmeny,
    OBCAN_DatZmeny,
    OBCAN_AdrObec AS OBCAN_Adr1Obec
FROM tbOBCAN_NEW
WHERE OBCAN_Id = @id

UNION ALL

SELECT
    OBCAN_Id,
    OBCAN_Prijmeni,
    OBCAN_Jmeno,
    OBCAN_RC,
    OBCAN_Titul,
    OBCAN_RodneJm,
    OBCAN_CisORP,
    OBCAN_Adr1Ulice,
    OBCAN_Adr1PSC,
    OBCAN_Adr1Cp,
    OBCAN_UsrZmeny,
    OBCAN_DatZmeny,
    OBCAN_Adr1Obec
FROM tbOBCAN
WHERE OBCAN_Id = @id
  AND NOT EXISTS (
      SELECT 1 FROM tbOBCAN_NEW WHERE OBCAN_Id = @id
);
";


            Zaznam _zaznam = new();
            Obcan _obcan = new();

            using (SqlConnection conn = new(CONNECTION_STRING))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new(sqlObcan, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string[] data = ReaderToStringArray(reader, "", "OBCAN_Id", "OBCAN_Prijmeni", "OBCAN_Jmeno", "OBCAN_Titul", "OBCAN_RC", "OBCAN_RodneJm",
                                                                "OBCAN_Adr1Ulice", "OBCAN_Adr1Cp", "OBCAN_Adr1Obec", "OBCAN_Adr1PSC", "OBCAN_CisORP");

                            int obcan_id = Convert.ToInt32(data[0]);
                            _obcan = new(data[1], data[2], data[3], data[4], data[5],
                                         new(data[6], data[7], data[8], data[9]),
                                         data[10], obcan_id);
                        }
                    }
                }
            }
            string sql = @"SELECT TOP 1 *
            FROM (
                -- New table rows, highest priority
                SELECT
                    o.OBCAN_CisORP,
                    o.OBCAN_UsrZmeny,
                    o.OBCAN_DatZmeny,
                    r.RIZENI_Podnik AS RIZENI_IdPodniku,
                    r.RIZENI_DatNastupu1,
                    r.RIZENI_DatNastupu2,
                    r.RIZENI_Datum,
                    r.RIZENI_DatVystaveno,
                    r.RIZENI_Stav,
                    r.RIZENI_Id,
                    c.CIS_Name AS RIZENI_STAV_NAME,
                    r.RIZENI_Stav AS RIZENI_STAV_CODE,
                    o.OBCAN_Id,
                    (SELECT TOP 1 ORP_Name FROM tbORP WHERE ORP_CisORP = o.OBCAN_CisORP) AS ORGANIZACE_NAME,
                    (SELECT TOP 1 ORP_SidloNazev FROM tbORP WHERE ORP_CisORP = o.OBCAN_CisORP) AS ORGANIZACE_SIDLO_NAZEV,
                    r.RIZENI_Podnik AS PODNIK_NAZEV1,
                    NULL AS PODNIK_NAZEV2,
                    1 AS priority
                FROM tbRIZENI_NEW r
                JOIN tbOBCAN_NEW o ON o.OBCAN_RC = r.RIZENI_RC
                JOIN tbCIS c ON r.RIZENI_Stav = c.CIS_Kod
                WHERE r.RIZENI_RC = (SELECT OBCAN_RC FROM tbOBCAN_NEW WHERE OBCAN_Id = @id)

                UNION ALL

                -- Old table rows, fallback if no new found
                SELECT
                    o.OBCAN_CisORP,
                    o.OBCAN_UsrZmeny,
                    o.OBCAN_DatZmeny,
                    CAST(r.RIZENI_IdPodniku AS NVARCHAR(40)) AS RIZENI_IdPodniku_Nvarchar,
                    r.RIZENI_DatNastupu1,
                    r.RIZENI_DatNastupu2,
                    r.RIZENI_Datum,
                    r.RIZENI_DatVystaveno,
                    r.RIZENI_Stav,
                    r.RIZENI_Id,
                    c.CIS_Name AS RIZENI_STAV_NAME,
                    r.RIZENI_Stav AS RIZENI_STAV_CODE,
                    o.OBCAN_Id,
                    (SELECT TOP 1 ORP_Name FROM tbORP WHERE ORP_CisORP = o.OBCAN_CisORP) AS ORGANIZACE_NAME,
                    (SELECT TOP 1 ORP_SidloNazev FROM tbORP WHERE ORP_CisORP = o.OBCAN_CisORP) AS ORGANIZACE_SIDLO_NAZEV,
                    (SELECT TOP 1 PODNIK_OrgNazev1 FROM tbPODNIK WHERE PODNIK_Id = r.RIZENI_IdPodniku) AS PODNIK_NAZEV1,
                    (SELECT TOP 1 PODNIK_OrgNazev2 FROM tbPODNIK WHERE PODNIK_Id = r.RIZENI_IdPodniku) AS PODNIK_NAZEV2,
                    2 AS priority
                FROM tbRIZENI r
                JOIN tbOBCAN o ON r.RIZENI_Obcan_Id = o.OBCAN_Id
                JOIN tbCIS c ON r.RIZENI_Stav = c.CIS_Kod
                WHERE r.RIZENI_RC = (SELECT OBCAN_RC FROM tbOBCAN WHERE OBCAN_Id = @id)
            ) AS combined
            ORDER BY priority
            ";
            try
            {
                using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _obcan.Id);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string[] data = ReaderToStringArray(reader, "",
        "RIZENI_IdPodniku", "RIZENI_DatNastupu1", "RIZENI_DatNastupu2", "RIZENI_Datum", "RIZENI_DatVystaveno",
        "RIZENI_STAV", "RIZENI_Id", "PODNIK_NAZEV1", "PODNIK_NAZEV2", "ORGANIZACE_NAME", "ORGANIZACE_SIDLO_NAZEV",
        "OBCAN_UsrZmeny", "OBCAN_DatZmeny");


                                int _id = Convert.ToInt32(data[6]);

                                Rizeni r = new(data[0], data[1], data[2], data[3], data[4], data[5], _id);

                                _obcan.PodnikNazev1 = data[7];
                                _obcan.PodnikNazev2 = data[8];

                                string organizaceText = $"{data[9]} ({data[10]})";

                                _zaznam = new(_obcan, r, organizaceText, data[11], data[12]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete record with ID {id}: {ex.Message}", ex);
            }

            return _zaznam;
        }


        /// <summary>
        /// Nacte zaznamy jejichz ID je v rozmezi start a end (vcetne v obou pripadech)
        /// </summary>
        /// <param name="start">Pocatecni ID</param>
        /// <param name="end">Konecne ID</param>
        /// <returns>Vraci seznam zaznamu s ID obcanu v rozmezi start a end (vcetne)</returns>
        /// 
        public static async Task<List<Zaznam>> LoadZaznamyBetweenId(string start, string end)
        {
            List<Zaznam> _zaznamy = new();

            int _start = Convert.ToInt32(start);
            int _end = Convert.ToInt32(end);

            int[] _ids = new int[_end - _start];

            for (int i = 0; i < (_end - _start); i++)
            {
                _ids[i] = _start + i;
            }

            Zaznam[] _z = await Task.WhenAll(_ids.Select(i => LoadSingleZaznamById(i.ToString())));

            foreach (Zaznam z in _z)
            {
                _zaznamy.Add(z);
            }

            return _zaznamy;
        }

        /*public async Task<List<Obcan>> LoadObcaneBetweenId(string start, string end)
        {
            List<Obcan> _obcane;

            int _start = Convert.ToInt32(start);
            int _end = Convert.ToInt32(end);

            _obcane = await LoadObcaneListQuery(_start, _end);

            return _obcane;
        }*/

        /// <summary>;
        /// Nacte vsechny podniky
        /// </summary>
        /// <returns>Vraci seznam vsech podniku</returns>
        public static async Task<List<Podnik>> LoadPodniky()
        {
            string sqlPodniky = @"SELECT 
                                tbPODNIK.PODNIK_Id,
                                tbPODNIK.PODNIK_OrgNazev1,
                                tbPODNIK.PODNIK_OrgNazev2,
                                tbPODNIK.PODNIK_OrgUlice,
                                tbPODNIK.PODNIK_OrgObec,
                                tbPODNIK.PODNIK_OrgPSC
                                FROM tbPODNIK
                                ORDER BY
                                tbPODNIK.PODNIK_OrgNazev1";

            List<Podnik> _podniky = new();

            using (SqlConnection conn = new(CONNECTION_STRING))
            {
                SqlCommand cmd = new(sqlPodniky, conn);
                int result = LoadData(conn, cmd).Result;
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string[] data = ReaderToStringArray(reader, "", "PODNIK_Id", "PODNIK_OrgNazev1", "PODNIK_OrgNazev2", "PODNIK_OrgUlice", "PODNIK_OrgObec", "PODNIK_OrgPSC");

                    Podnik p = new(data[0], data[1], data[2], data[3], data[4], data[5], null, null, null);

                    _podniky.Add(p);
                }
            }

            return _podniky;
        }

        /// <summary>
        /// Nacte vsechny stavy
        /// </summary>
        /// <returns>Vraci seznam vsech stavu</returns>
        public static async Task<List<CisStav>> LoadStavy()
        {
            string sqlStavy = @"Select
                                tbCIS.CIS_Kod, tbCIS.CIS_NAME
                                FROM tbCIS
                                WHERE
                                tbCIS.CIS_Id BETWEEN 1001 AND 1026";

            List<CisStav> _stavy = new();

            using (SqlConnection conn = new(CONNECTION_STRING))
            {
                SqlCommand cmd = new(sqlStavy, conn);
                int result = LoadData(conn, cmd).Result;
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string[] data = ReaderToStringArray(reader, "", "CIS_Kod", "CIS_Name");

                    CisStav cs = new(data[0], data[1]);

                    _stavy.Add(cs);
                }
            }

            return _stavy;
        }

        /// <summary>
        /// Nacte  vsechny organizace
        /// </summary>
        /// <returns>Vraci seznam vsech organizaci</returns>
        public static async Task<List<Organizace>> LoadOrganizace()
        {
            string sqlOrganizace = @"Select
                                      ORP_CisORP, 
                                      ORP_Name, 
                                      ORP_SidloNazev, 
                                      ORP_SidloUlice, 
                                      ORP_SidloPSC, 
                                      ORP_SidloObec, 
                                      ORP_PracOdbor
                                      from tbORP
                                      WHERE
                                      ORP_OkrOrp like 'ORP'";

            List<Organizace> _organizace = new();

            using (SqlConnection conn = new(CONNECTION_STRING))
            {
                SqlCommand cmd = new(sqlOrganizace, conn);
                int result = LoadData(conn, cmd).Result;
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string[] data = ReaderToStringArray(reader, "", "ORP_CisORP", "ORP_Name", "ORP_SidloNazev", "ORP_SidloUlice", "ORP_SidloPSC", "ORP_SidloObec", "ORP_PracOdbor");

                    Organizace o = new(data[0], data[1], data[2], data[3], data[4], data[5], data[6]);

                    _organizace.Add(o);
                }
            }

            return _organizace;
        }

        /// <summary>
        /// Pripoji se k databazi a nacte data
        /// </summary>
        /// <param name="conn">Pripojeni k databazi</param>
        /// <param name="cmd">Objekt tridy SqlCommand, ktery obsahuje SQL dotaz</param>
        /// <returns>Vraci integer</returns>
        private static async Task<int> LoadData(SqlConnection conn, SqlCommand cmd, bool closeConn = false)
        {
            if (conn.State == ConnectionState.Closed)
            {
                await conn.OpenAsync();
            }

            // Check if the command is SELECT
            if (cmd.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                cmd.CommandText.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            {

            }
            else
            {
                await cmd.ExecuteNonQueryAsync();
            }

            if (closeConn)
            {
                await conn.CloseAsync();
            }

            return 1;
        }


        /// <summary>
        /// Vycisti text od nebezpecnych 'escape' znaku
        /// </summary>
        /// <param name="text">Vstupni text</param>
        /// <returns>Vraci vycisteny text</returns>
        private static string MySQLEscape(string text)
        {
            if (text == null || text == "")
            {
                return "";
            }

            return Regex.Replace(text, @"[\x00'""\b\n\r\t\cZ\\%_]",
                delegate (Match match)
                {
                    string v = match.Value;

                    return v switch
                    {
                        // null char
                        "\x00" => "\\0",
                        // backspace
                        "\b" => "\\b",
                        //\n
                        "\n" => "\\n",
                        //\r
                        "\r" => "\\r",
                        //tab
                        "\t" => "\\t",
                        // ctrl-z
                        "\u001A" => "\\Z",
                        _ => "\\" + v,
                    };
                });
        }

        public int GetRokNarozeni(string rc)
        {
            // Rodné číslo má formát XXXXXX/XXXX, takže vybereme první část
            string rcBezLomitka = rc.Split('/')[0];

            // Poslední dvě číslice jsou rok narození
            int rokNarozeni = int.Parse(rcBezLomitka.Substring(0, 2));

            // Pokud je rok menší než 54, jedná se o rok 2000-2053, jinak o rok 1900-1999
            if (rokNarozeni < 54)
            {
                rokNarozeni += 2000;
            }
            else
            {
                rokNarozeni += 1900;
            }

            return rokNarozeni;
        }

        public async Task<List<Obcan>> LoadObcaneByRokNarozeni(int startRok, int endRok)
        {
            List<Obcan> _obcane = new();

            string sql = @"SELECT
                 OBCAN_Id,
                 OBCAN_Prijmeni,
                 OBCAN_Jmeno,
                 OBCAN_RC,
                 OBCAN_Titul,
                 OBCAN_RodneJm,
                 OBCAN_CisORP,
                 OBCAN_Adr1Ulice,
                 OBCAN_Adr1PSC,
                 OBCAN_Adr1Cp,
                 OBCAN_Adr1Obec
                 FROM tbOBCAN
                 WHERE SUBSTRING(OBCAN_RC, 1, 2) BETWEEN @startRok AND @endRok";

            using (SqlConnection conn = new(CONNECTION_STRING))
            {
                await conn.OpenAsync();

                SqlCommand cmd = new(sql, conn);
                cmd.Parameters.AddWithValue("@startRok", startRok);
                cmd.Parameters.AddWithValue("@endRok", endRok);

                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string[] data = ReaderToStringArray(reader, "", "OBCAN_Id", "OBCAN_Prijmeni", "OBCAN_Jmeno", "OBCAN_Titul", "OBCAN_RC", "OBCAN_RodneJm",
                        "OBCAN_Adr1Ulice", "OBCAN_Adr1Cp", "OBCAN_Adr1Obec", "OBCAN_Adr1PSC", "OBCAN_CisORP");

                    if (data.Length < 11)
                    {
                        throw new Exception("Chyba: Očekávalo se 11 hodnot, ale načteno bylo méně.");
                    }

                    string rodneCislo = string.IsNullOrEmpty(data[4]) ? "Neznámé" : data[4];

                    // Zkusíme převést datum narození, pokud je rodné číslo validní
                    string datumNarozeni;

                    try
                    {
                        datumNarozeni = DataHandler.ConvertRCToDatumNarozeni(rodneCislo);
                    }
                    catch
                    {
                        //datumNarozeni = "Neznámé datum"; // Pokud je RC nevalidní, nastavíme tuto hodnotu
                        datumNarozeni = GetDatumNarozeniFromRC(rodneCislo);
                    }

                    Obcan _obcan = new(data[1], data[2], data[3], rodneCislo, data[4],
                new(data[6], data[7], data[8], data[9]), data[10], Convert.ToInt32(data[0]));

                    _obcane.Add(_obcan);
                }

                await conn.CloseAsync();
            }

            return _obcane;
        }

        // Nová metoda pro zjištění datumu narození z rodného čísla bez volání ConvertRCToDatumNarozeni
        private static string GetDatumNarozeniFromRC(string RC)
        {
            if (string.IsNullOrEmpty(RC) || RC.Length < 6)
            {
                return "Neznámé datum";
            }

            string rok = "19" + RC[0].ToString() + RC[1].ToString();
            string mesic = RC[2].ToString() + RC[3].ToString();
            string den = RC[4].ToString() + RC[5].ToString();

            // Ošetření pro ženy
            if (int.Parse(mesic) > 50)
            {
                mesic = (int.Parse(mesic) - 50).ToString("D2");
            }

            return $"{den}.{mesic}.{rok}";
        }


        /// <summary>
        /// Metoda, ktera vrati hodnotu z vysledku databazoveho dotazu za pomoci klice.
        /// </summary>
        /// <param name="reader">Objekt tridy SqlDataReader, ktery obsahuje informace o vysledku dotazu</param>
        /// <param name="key">Klic, podle ktereho ma byt hodnota nalezena</param>
        /// <param name="backupValue">Zalozni hodnota, ktera se pouzije v pripade, ze neexistuje hodnota pro dany klic</param>
        /// <returns>Vraci hodnotu, ktera byla nactena z databaze</returns>
        private static string ReaderToString(SqlDataReader reader, string key, string backupValue = "")
        {
            return reader[key].ToString() ?? backupValue;
        }

        /// <summary>
        /// Metoda, ktera vraci pole hodnot z vysledku databazoveho dotazu za pomoci klicu.
        /// </summary>
        /// <param name="reader">Objekt tridy SqlDataReader, ktery obsahuje informace o vysledku dotazu</param>
        /// <param name="backupvalue">Zalozni hodnota, ktera se pouzije v pripade, ze neexistuje hodnota pro dany klic</param>
        /// <param name="keys">Nekonecne dlouha rada klicu, podle kterych budou vraceny hodnoty ve stejnem poradi</param>
        /// <returns>Vraci pole hodnot, ktere byly nacteny z databaze</returns>
        private static string[] ReaderToStringArray(SqlDataReader reader, string backupvalue, params string?[] keys)
        {
            string[] values = new string[keys.Length];

            for (int i = 0; i < keys.Length; i++)
            {
                values[i] = ReaderToString(reader, keys[i] ?? "", backupvalue);
            }

            return values;
        }
    }
}
