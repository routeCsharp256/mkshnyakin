﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using OzonEdu.MerchandiseService.Domain.AggregationModels.MerchPackItemAggregate;
using OzonEdu.MerchandiseService.Domain.AggregationModels.MerchRequestAggregate;
using OzonEdu.MerchandiseService.Infrastructure.Repositories.Infrastructure.Interfaces;

namespace OzonEdu.MerchandiseService.Infrastructure.Repositories.Implementation
{
    public class MerchPackItemPostgreSqlRepository : IMerchPackItemRepository
    {
        private const int Timeout = 5;
        private readonly IChangeTracker _changeTracker;
        private readonly IDbConnectionFactory<NpgsqlConnection> _dbConnectionFactory;

        public MerchPackItemPostgreSqlRepository(
            IDbConnectionFactory<NpgsqlConnection> dbConnectionFactory,
            IChangeTracker changeTracker)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _changeTracker = changeTracker;
        }

        public async Task<MerchPackItem> CreateAsync(
            MerchPackItem itemToCreate,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                insert into merch_pack_items (name, sku)
                values  (@Name, @Sku) returning id;
            ";

            var parameters = new
            {
                Name = itemToCreate.ItemName.Value,
                Sku = itemToCreate.Sku.Id,
            };

            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            var identity = await connection.QuerySingleOrDefaultAsync<long>(commandDefinition);
            if (identity != default)
            {
                itemToCreate.SetId(identity);
            }

            _changeTracker.Track(itemToCreate);
            return itemToCreate;
        }

        public async Task<MerchPackItem> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                select * 
                from merch_pack_items
                where id = @Id
                limit 1;
            ";

            var parameters = new
            {
                Id = id
            };

            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            var merchPackItemModel =
                await connection.QuerySingleOrDefaultAsync<Models.MerchPackItem>(commandDefinition);
            var merchPackItem = merchPackItemModel is null
                ? null
                : CreateMerchPackItemByModel(merchPackItemModel);
            _changeTracker.Track(merchPackItem);
            return merchPackItem;
        }

        public async Task<MerchPackItem> UpdateAsync(
            MerchPackItem itemToUpdate,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                update merch_pack_items
                set
                    name = @Name,
                    sku = @Sku
                where id = @Id;
            ";

            var parameters = new
            {
                Id = itemToUpdate.Id,
                Name = itemToUpdate.ItemName.Value,
                Sku = itemToUpdate.Sku.Id,
            };

            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            await connection.ExecuteAsync(commandDefinition);
            _changeTracker.Track(itemToUpdate);
            return itemToUpdate;
        }

        public async Task<MerchPackItem> DeleteAsync(
            MerchPackItem itemToDelete,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                delete from merch_pack_items
                where id = @Id;
            ";

            var parameters = new
            {
                Id = itemToDelete.Id,
            };

            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            await connection.ExecuteAsync(commandDefinition);
            _changeTracker.Track(itemToDelete);
            return itemToDelete;
        }

        public async Task<IReadOnlyCollection<MerchPackItem>> FindByMerchTypeAsync(
            RequestMerchType requestMerchType,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                select
                    i.id,
                    i.name,
                    i.sku
                from merch_type_to_items_relations as r
                inner join merch_pack_items i on i.id = r.merch_pack_item_id 
                where
                    r.merch_type = @MerchType;
            ";

            var parameters = new
            {
                MerchType = requestMerchType.Id
            };
            
            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            var merchPackItems = await QueryMerchPackItemsAsync(connection, commandDefinition);

            foreach (var merchPackItem in merchPackItems)
            {
                _changeTracker.Track(merchPackItem);
            }

            return merchPackItems;
        }

        public async Task<IReadOnlyCollection<RequestMerchType>> FindMerchTypesBySkuAsync(
            IEnumerable<long> skuIds,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                select distinct 
                    r.merch_type
                from merch_pack_items as i
                inner join merch_type_to_items_relations as r on i.id = r.merch_pack_item_id 
                where
                    i.sku = any(@Skus);
            ";

            var parameters = new
            {
                Skus = skuIds.ToArray()
            };
            
            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);

            var requestMerchTypeIds = await connection.QueryAsync<int>(commandDefinition);
            var requestMerchTypes = requestMerchTypeIds.Select(RequestMerchType.Create)
                .ToList()
                .AsReadOnly() as IReadOnlyCollection<RequestMerchType>;
            return requestMerchTypes;
        }

        public async Task AddToPackAsync(
            RequestMerchType requestMerchType,
            IEnumerable<MerchPackItem> merchPackItems,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                insert into merch_type_to_items_relations (merch_type, merch_pack_item_id)
                values (@MerchType, @MerchPackItemId)
                on conflict do nothing;
            ";

            var parameters = merchPackItems.Select(x => new
            {
                MerchType = requestMerchType.Id,
                MerchPackItemId = x.Id
            }
            ).ToArray();
            
            var commandDefinition = new CommandDefinition(
                sql,
                parameters,
                commandTimeout: Timeout,
                cancellationToken: cancellationToken);

            var connection = await _dbConnectionFactory.CreateConnection(cancellationToken);
            await connection.ExecuteAsync(commandDefinition);
        }

        private static async Task<IReadOnlyCollection<MerchPackItem>> QueryMerchPackItemsAsync(
            NpgsqlConnection connection,
            CommandDefinition commandDefinition)
        {
            var merchPackItemModels = await connection.QueryAsync<Models.MerchPackItem>(commandDefinition);
            var merchPackItems = merchPackItemModels.Select(CreateMerchPackItemByModel)
                .ToList()
                .AsReadOnly() as IReadOnlyCollection<MerchPackItem>;

            return merchPackItems;
        }

        private static MerchPackItem CreateMerchPackItemByModel(Models.MerchPackItem merchPackItemModel)
        {
            var merchPackItem = new MerchPackItem(
                merchPackItemModel.Id,
                ItemName.Create(merchPackItemModel.Name),
                Sku.Create(merchPackItemModel.Sku)
            );
            return merchPackItem;
        }
    }
}