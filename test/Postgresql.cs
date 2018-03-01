using System;
using System.Data;
using Npgsql;

namespace NetBaltic.ServerProxy
{
	
	public class Postgresql {
		
		public NpgsqlConnection dbcon = null;
		private string connectionString = null;
		private NpgsqlCommand dbcmd = null;

		public static class Locks {
			public static readonly object Buffer = new object();
			public static readonly object MessagesSent = new object ();
			public static readonly object MessagesReceived = new object ();
		}

		public Postgresql () {
			connectionString =
				"Host=localhost;" +
				"Username=netbaltic;" +
				"Password=baltic;" +
				"Database=netbaltic;";
			dbcon = new NpgsqlConnection(connectionString);
		}

		public Postgresql (string host, string user, string password, string database) {
			connectionString =
				"Host=" + host + ";" +
				"Username=" + user + ";" +
				"Password=" + password + ";" +
				"Database=" + database + ";";
			dbcon = new NpgsqlConnection(connectionString);
		}

		public void Open() {
			dbcon.Open ();
		}

		public void Close() {
			if (dbcmd != null) {
				dbcmd.Dispose ();
				dbcmd = null;
			}
			dbcon.Close ();
		}

		public void Dispose() {
			if(dbcmd != null) {
				dbcmd.Dispose ();
				dbcmd = null;
			}
		}

		public Npgsql​Binary​Importer BeginBinaryImport (string sql) {
			return dbcon.BeginBinaryImport (sql);
		}

		public NpgsqlDataAdapter SelectAdapter (string sql, params NpgsqlParameter [] values) {
			NpgsqlDataAdapter adapter = new NpgsqlDataAdapter ();

			adapter.SelectCommand = new NpgsqlCommand (sql, dbcon);
			foreach (var param in values) {
				adapter.SelectCommand.Parameters.Add (param);
			}

			return adapter;
		}

		public NpgsqlDataAdapter InsertAdapter (string sql, params NpgsqlParameter [] values) {
			NpgsqlDataAdapter adapter = new NpgsqlDataAdapter ();

			adapter.InsertCommand = new NpgsqlCommand (sql, dbcon);
			foreach (var param in values) {
				adapter.InsertCommand.Parameters.Add (param);
			}

			return adapter;
		}

		public NpgsqlDataAdapter DeleteAdapter (string sql, params NpgsqlParameter [] values) {
			NpgsqlDataAdapter adapter = new NpgsqlDataAdapter ();

			adapter.DeleteCommand = new NpgsqlCommand (sql, dbcon);
			foreach (var param in values) {
				adapter.DeleteCommand.Parameters.Add (param);
			}

			return adapter;
		}

		public NpgsqlDataReader ParametrizedSql(string sql, params NpgsqlParameter[] values) {
			if (dbcmd == null) {
				if (dbcon == null) {
					throw new ArgumentNullException ("dbcon");
				} else {
					dbcmd = dbcon.CreateCommand ();
				}
			}

			dbcmd.CommandText = sql;
			foreach (var param in values) {
				dbcmd.Parameters.Add (param);
			}
			return dbcmd.ExecuteReader ();
		}

		public NpgsqlDataReader Sql(string sql) {
			if (dbcmd == null) {
				if (dbcon == null) {
					throw new ArgumentNullException ("dbcon");
				} else {
					dbcmd = dbcon.CreateCommand ();
				}
			}

			dbcmd.CommandText = sql;
			return dbcmd.ExecuteReader ();
		}
	}
}

