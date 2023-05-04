﻿using FluentStorage.Blobs;
using FluentStorage.ConnectionString;
using FluentStorage.Messaging;

namespace FluentStorage.Gcp.CloudStorage {
	class Module : IExternalModule, IConnectionFactory {
		public IConnectionFactory ConnectionFactory => new Module();

		public IBlobStorage CreateBlobStorage(StorageConnectionString connectionString) {
			if (connectionString.Prefix == "google.storage") {
				connectionString.GetRequired("bucket", true, out string bucketName);
				connectionString.GetRequired("cred", true, out string base64EncodedJson);

				return StorageFactory.Messages.RabbitMq(bucketName, base64EncodedJson, true);
			}

			return null;
		}

		public IMessenger CreateMessenger(StorageConnectionString connectionString) => null;
	}
}
