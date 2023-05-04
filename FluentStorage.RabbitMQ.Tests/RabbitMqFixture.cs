using Bogus;

using System.Threading.Tasks;

using Testcontainers.RabbitMq;

using Xunit;

namespace FluentStorage.RabbitMQ.Tests.Messaging {
	public class RabbitMqFixture : IAsyncLifetime {

		private readonly static Faker _faker = new();
		private readonly RabbitMqContainer _rabbitMq;

		public RabbitMqFixture() {
			_rabbitMq = new RabbitMqBuilder()
				.WithUsername(_faker.Internet.UserName())
				.WithPassword(_faker.Internet.Password())
				.Build();
		}

		///<inheritdoc/>
		public async Task DisposeAsync() => await _rabbitMq.StopAsync().ConfigureAwait(false);

		///<inheritdoc/>
		public async Task InitializeAsync() => await _rabbitMq.StartAsync().ConfigureAwait(false);
	}
}
