using FluentStorage.Blobs;
using FluentStorage.ConnectionString;
using FluentStorage.Messaging;

namespace FluentStorage.RabbitMQ {
	class Module : IExternalModule, IConnectionFactory {
		public IConnectionFactory ConnectionFactory => new Module();

		public IBlobStorage CreateBlobStorage(StorageConnectionString connectionString) => null;

		///<inheritdoc/>
		public IMessenger CreateMessenger(StorageConnectionString connectionString) {
			IRabbitMqMessenger messenger = null;

			if (connectionString.Prefix == "rabbitmq") {
				messenger = StorageFactory.Messages.RabbitMq(connectionString);
			}

			return messenger;
		}
	}
}
