using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RDBMS.Engine
{
    public class Table
    {
        public TableSchema Schema { get; set; }
        private readonly string _filePath;
        // The Index: Maps Primary Key (Int) -> File Offset (Long)
        public Dictionary<int, long> PrimaryKeyIndex { get; private set; } = new();

        public Table(string name, List<ColumnDef> columns)
        {
            Schema = new TableSchema { Name = name, Columns = columns };
            _filePath = $"{name}.db"; // Saves to bin/Debug/net9.0/users.db
            
            if (!File.Exists(_filePath)) InitFile();
            LoadIndex(); // Build index on startup
        }

        private void InitFile()
        {
            using var fs = new FileStream(_filePath, FileMode.Create);
            // In a real DB, we would write the schema header here. 
            // For this challenge, we assume schema is defined in code/memory.
        }

        // REQ: Indexing & Primary Key
        private void LoadIndex()
        {
            PrimaryKeyIndex.Clear();
            if (!File.Exists(_filePath)) return;

            using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            try
            {
                while (fs.Position < fs.Length)
                {
                    long currentPos = fs.Position;
                    bool isDeleted = reader.ReadBoolean();
                    
                    // Read the ID (First column is always ID in our simplified design)
                    int id = reader.ReadInt32();

                    if (!isDeleted)
                    {
                        PrimaryKeyIndex[id] = currentPos;
                    }

                    // Skip the rest of the row to get to the next one
                    foreach (var col in Schema.Columns)
                    {
                        if (col.Name == "id") continue; // Already read ID
                        if (col.Type == DbType.Int) reader.ReadInt32();
                        else if (col.Type == DbType.String) reader.ReadString();
                    }
                }
            }
            catch (EndOfStreamException) { }
        }

        // REQ: CRUD - Create
        public void Insert(Row row)
        {
            if (PrimaryKeyIndex.ContainsKey(row.Id))
                throw new Exception($"Duplicate Primary Key: {row.Id}");

            using var fs = new FileStream(_filePath, FileMode.Append);
            using var writer = new BinaryWriter(fs);

            long pos = fs.Position; // Capture where we are writing
            
            writer.Write(false); // IsDeleted = false
            writer.Write(row.Id); // Always write ID first

            foreach (var col in Schema.Columns)
            {
                if (col.Name == "id") continue;
                
                if (col.Type == DbType.Int) 
                    writer.Write((int)row.Data[col.Name]);
                else 
                    writer.Write((string)row.Data[col.Name]);
            }

            // Update Index immediately
            PrimaryKeyIndex[row.Id] = pos;
        }

        // REQ: CRUD - Read (Indexed!)
        public Row? SelectById(int id)
        {
            if (!PrimaryKeyIndex.ContainsKey(id)) return null;

            long offset = PrimaryKeyIndex[id];
            
            using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);
            
            fs.Seek(offset, SeekOrigin.Begin); // JUMP directly to data!

            var row = new Row { Data = new Dictionary<string, object>() };
            row.IsDeleted = reader.ReadBoolean();
            if (row.IsDeleted) return null;

            row.Id = reader.ReadInt32();
            row.Data["id"] = row.Id;

            foreach (var col in Schema.Columns)
            {
                if (col.Name == "id") continue;
                if (col.Type == DbType.Int) row.Data[col.Name] = reader.ReadInt32();
                else row.Data[col.Name] = reader.ReadString();
            }

            return row;
        }

        public List<Row> SelectAll()
        {
            var rows = new List<Row>();
            using var fs = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            while (fs.Position < fs.Length)
            {
                // Similar read logic, just in a loop
                bool isDeleted = reader.ReadBoolean();
                int id = reader.ReadInt32();
                var row = new Row { Id = id, Data = new Dictionary<string, object>() {{ "id", id }} };

                foreach (var col in Schema.Columns)
                {
                    if (col.Name == "id") continue;
                    if (col.Type == DbType.Int) row.Data[col.Name] = reader.ReadInt32();
                    else row.Data[col.Name] = reader.ReadString();
                }

                if (!isDeleted) rows.Add(row);
            }
            return rows;
        }
    }
}