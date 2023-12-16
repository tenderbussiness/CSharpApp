﻿using _3.Database.Entities;
using _3.Database.Interfaces;
using Bogus;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Globalization;

namespace _3.Database
{
    /// <summary>
    /// Керування таблицею і даними по клієнтів
    /// </summary>
    public class ClientManager: IManager<Client>
    {
        private SqlConnection _conn;
        private readonly IManager<Profession> _proffesionManager;
        /// <summary>
        /// Підлкючення до конкретної бази даних на сервері
        /// </summary>
        /// <param name="nameDatabase">Назва бази даних</param>
        public delegate void RecordAddedNumber(int recordNumber);
        public event RecordAddedNumber RecordAdded;
        public ClientManager(string nameDatabase)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration configuration = builder.Build();

            string conStr = configuration.GetConnectionString("MSSQLServerConnection") ?? "Data Source=.;Integrated Security=True;";
            conStr += $"Initial Catalog={nameDatabase};";

            _conn = new SqlConnection(conStr);
            _conn.Open();
            _proffesionManager = new ProfessionManager(_conn);
        }

        /// <summary>
        /// Повертаємо список усіх кліжнтів
        /// </summary>
        public List<Client> GetList()
        {
            List<Client> list = new List<Client>();
            //показати список БД
            string sql = "SELECT c.Id, c.ProfessionId, c.FirstName, c.LastName, c.Phone, c.DateOfBirth, " +
                "c.CreatedDate, c.Sex, p.Name as ProfessionName " +
                "FROM tblClients  as c, tblProfessions p " +
                "WHERE c.ProfessionId = p.Id;";
            SqlCommand sqlCommand = _conn.CreateCommand();
            sqlCommand.CommandText = sql;
            //Результа сервера будемо читати через SqlDataReeader
            using (SqlDataReader reader = sqlCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    Client entity = new Client();
                    entity.Id = int.Parse(reader["Id"].ToString());
                    entity.LastName = reader["LastName"].ToString();
                    entity.FirstName = reader["FirstName"].ToString();
                    entity.Phone = reader["Phone"].ToString();
                    entity.DateOfBirth = reader["DateOfBirth"].ToString();
                    entity.CreatedDate = reader["CreatedDate"].ToString();
                    entity.Sex = Boolean.Parse(reader["Sex"].ToString());
                    entity.ProfessionName = reader["ProfessionName"].ToString();
                    list.Add(entity);
                }
            }

            return list;
        }

        /// <summary>
        /// Додати клієнта
        /// </summary>
        public void Insert()
        {
            Client c = new Client();
            Console.WriteLine("Вкажіть прізвище клієнта:");
            c.LastName = Console.ReadLine();
            Console.WriteLine("Вкажіть ім'я клієнта:");
            c.FirstName = Console.ReadLine();
            Console.WriteLine("Вкажіть телефон клієнта:");
            c.Phone = Console.ReadLine();
            Console.WriteLine("Вкажіть дату народження клієнта(2004-12-08):");
            c.DateOfBirth = Console.ReadLine();
            var date = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            c.CreatedDate = date;
            Console.WriteLine("Беріть професію:");
            foreach (var p in _proffesionManager.GetList())
            {
                Console.WriteLine(p);
            }
            Console.Write("->_");
            c.ProfessionId = int.Parse(Console.ReadLine());

            //2004-12-08
            //2023-09-10 11:15:22
            string sql = "INSERT INTO tblClients " +
                "(FirstName, ProfessionId, LastName, Phone, DateOfBirth, CreatedDate, Sex) " +
                $"VALUES(N'{c.FirstName}', {c.ProfessionId}, N'{c.LastName}', " +
                $"N'{c.Phone}', '{c.DateOfBirth}', '{c.CreatedDate}', {(c.Sex?1:0)});";
            SqlCommand sqlCommand = _conn.CreateCommand(); //окманди виконуєються на основі підлкючення
            sqlCommand.CommandText = sql; //текст команди
            //виконати комнаду до сервера
            sqlCommand.ExecuteNonQuery();
        }
         protected virtual void  OnRecordAdded()
        {
            RecordAdded?.Invoke(GetLastRecordNumber());
        }
        private int GetLastRecordNumber()
        {
            string sql = "SELECT MAX(Id) FROM tblClients;";
            SqlCommand sqlCommand = _conn.CreateCommand();
            sqlCommand.CommandText = sql;
            object result = sqlCommand.ExecuteScalar();
            if (result!=null&&int.TryParse(result.ToString(),out int lastRecordNum))
            {
                return lastRecordNum;
            }
            return 0;
        }
        public void Dispose()
        {
            _conn.Close();
        }

        public Client GetById(int id)
        {
            throw new NotImplementedException();
        }

        public void Delete(Client entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Client entity)
        {
            throw new NotImplementedException();
        }

        public void GenerateRandom(int count)
        {
            var faker = new Faker<Client>("uk")
               .RuleFor(u => u.FirstName, f => f.Name.FirstName())
               .RuleFor(u => u.LastName, f => f.Name.LastName())
               .RuleFor(u => u.CreatedDate, f =>
               {
                   return DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
               })
               .RuleFor(u => u.DateOfBirth, f =>
               {
                   return DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd hh:mm:ss");
               })
               .RuleFor(u => u.Phone, f => f.Phone.PhoneNumber());
            for (int i = 0; i < count; i++)
            {
                Client c = faker.Generate();
                c.ProfessionId = 1;
                string sql = "INSERT INTO tblClients " +
                "(FirstName, ProfessionId, LastName, Phone, DateOfBirth, CreatedDate, Sex) " +
                $"VALUES(N'{c.FirstName}', {c.ProfessionId}, N'{c.LastName}', " +
                $"N'{c.Phone}', '{c.DateOfBirth}', '{c.CreatedDate}', {(c.Sex ? 1 : 0)});";
                SqlCommand sqlCommand = _conn.CreateCommand(); //окманди виконуєються на основі підлкючення
                sqlCommand.CommandText = sql; //текст команди
                                              //виконати комнаду до сервера
                sqlCommand.ExecuteNonQuery();
            }
        }
    }
}
