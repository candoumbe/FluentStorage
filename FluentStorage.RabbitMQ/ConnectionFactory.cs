using FluentStorage.Blobs;
using FluentStorage.ConnectionString;
using FluentStorage.Messaging;
using FluentStorage.RabbitMQ.Messaging;

namespace FluentStorage.SFTP {
	/// <summary>
	/// The <see cref="T:FluentStorage.SFTP.ConnectionFactory"/> class is responsible for creating
	/// <see cref="T:FluentStorage.SFTP.SshNetSftpBlobStorage"/> instances from supported connection strings.
	/// </summary>
	/// <seealso cref="T:FluentStorage.ConnectionString.IConnectionFactory" />
	class ConnectionFactory : IConnectionFactory {
		/// <summary>
		/// The default port for Kafka connections.
		/// </summary>
		public const ushort DefaultPort = 22;

		/// <summary>
		/// Creates a blob storage instance from the specified connection string if supported; Otherwise it returns null.
		/// </summary>
		/// <param name="connectionString">The connection string to parse.</param>
		/// <returns></returns>
		public IBlobStorage CreateBlobStorage(StorageConnectionString connectionString) => null;

		///<inheritdoc/>
		public IMessenger CreateMessenger(StorageConnectionString connectionString) {
			if (connectionString.Prefix == "kafka") {
				connectionString.GetRequired("host", true, out string host);
				connectionString.GetRequired("user", true, out string user);
				connectionString.GetRequired("password", true, out string password);
				ushort port = ushort.TryParse(connectionString.Get("port"), out port) ? port : DefaultPort;

				return new RabbitMQMessenger(host, port, user, password);
			}
			return null;
		}
	}
}
