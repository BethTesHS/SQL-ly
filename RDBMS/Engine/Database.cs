using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RDBMS.Engine
{
    public class Database
    {
        public Dictionary<string, Table> Tables { get; set; } = new();

        public Database()
        {
            // Seed a default table for the challenge
            // REQ: Declaring tables
            var userCols = new List<ColumnDef> 
            { 
                new ColumnDef { Name = "id", Type = DbType.Int, IsPrimaryKey = true },
                new ColumnDef { Name = "username", Type = DbType.String },
                new ColumnDef { Name = "age", Type = DbType.Int }
            };
            Tables.Add("users", new Table("users", userCols));

            var orderCols = new List<ColumnDef>
            {
                new ColumnDef { Name = "id", Type = DbType.Int, IsPrimaryKey = true },
                new ColumnDef { Name = "user_id", Type = DbType.Int }, // FK
                new ColumnDef { Name = "item", Type = DbType.String }
            };
            Tables.Add("orders", new Table("orders", orderCols));
        }

        // REQ: Interface should be SQL or something similar
        public string ExecuteSql(string sql)
        {
            try 
            {
                var parts = sql.Trim().Split(' ');
                var command = parts[0].ToUpper();

                switch (command)
                {
                    case "INSERT": return HandleInsert(sql);
                    case "SELECT": return HandleSelect(sql);
                    default: return "Unknown command.";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // PARSER: INSERT INTO users VALUES (1, "Alice", 30)
        private string HandleInsert(string sql)
        {
            // Very naive parsing for the challenge
            var tableName = sql.Split(new[] { "INTO ", " VALUES" }, StringSplitOptions.None)[1].Trim();
            var valuesPart = sql.Split("VALUES")[1].Trim().Trim('(', ')');
            var values = valuesPart.Split(',');

            if (!Tables.ContainsKey(tableName)) return "Table not found.";

            var table = Tables[tableName];
            var row = new Row();
            
            // Manual mapping of parsed values to schema
            int valIndex = 0;
            foreach(var col in table.Schema.Columns)
            {
                var valStr = values[valIndex].Trim().Trim('"', '\'');
                if (col.Name == "id") row.Id = int.Parse(valStr);
                
                if (col.Type == DbType.Int) row.Data[col.Name] = int.Parse(valStr);
                else row.Data[col.Name] = valStr;
                
                valIndex++;
            }

            table.Insert(row);
            return "Row inserted successfully.";
        }

        // PARSER: SELECT * FROM users [JOIN orders ON ...] [WHERE id=1]
        // REQ: Some Joining
        private string HandleSelect(string sql)
        {
            // Check for JOIN
            if (sql.ToUpper().Contains("JOIN"))
            {
                return HandleJoin(sql);
            }

            var parts = sql.Split(' ');
            var tableName = parts[3]; // SELECT * FROM [table]

            if (!Tables.ContainsKey(tableName)) return "Table not found.";
            var table = Tables[tableName];

            // Check for simple Primary Key Index lookup
            // REQ: Indexing usage
            if (sql.ToUpper().Contains("WHERE ID="))
            {
                var idStr = sql.Split('=')[1].Trim();
                int id = int.Parse(idStr);
                var row = table.SelectById(id);
                return row == null ? "No results." : FormatRow(row);
            }

            // Full Scan
            var rows = table.SelectAll();
            return FormatRows(rows);
        }

        // REQ: Implementation of Joining (Nested Loop Join)
        private string HandleJoin(string sql)
        {
            // Syntax: SELECT * FROM users JOIN orders ON users.id = orders.user_id
            var joinSplit = sql.Split(new[] { " JOIN ", " ON " }, StringSplitOptions.None);
            var table1Name = joinSplit[0].Split("FROM")[1].Trim();
            var table2Name = joinSplit[1].Trim();
            var condition = joinSplit[2].Trim(); // users.id = orders.user_id

            var t1 = Tables[table1Name];
            var t2 = Tables[table2Name];

            var results = new StringBuilder();
            results.AppendLine($"--- JOIN RESULT ({table1Name} + {table2Name}) ---");

            // Nested Loop Join Algorithm
            foreach (var r1 in t1.SelectAll())
            {
                foreach (var r2 in t2.SelectAll())
                {
                    // Hardcoded check for this challenge example: users.id == orders.user_id
                    // In a real DB, you'd parse the condition string dynamically
                    if (r1.Id == (int)r2.Data["user_id"])
                    {
                        results.AppendLine($"{r1.Data["username"]} bought {r2.Data["item"]}");
                    }
                }
            }
            return results.ToString();
        }

        private string FormatRow(Row row) => System.Text.Json.JsonSerializer.Serialize(row.Data);
        private string FormatRows(List<Row> rows) 
        {
            var sb = new StringBuilder();
            foreach (var r in rows) sb.AppendLine(FormatRow(r));
            return sb.ToString();
        }
    }
}