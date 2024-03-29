﻿using FluentMigrator;

namespace OzonEdu.MerchandiseService.Migrator.Migrations
{
    [Migration(1)]
    public class MerchRequests : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                create table if not exists merch_requests (
                    id            bigserial primary key,
                    employee_id   bigint    not null,
                    merch_type    integer   not null,
                    status        integer   not null,
                    mode          integer   not null,
                    give_out_date timestamp
                );

                create index merch_requests_merch_type_status_idx
                    on merch_requests (merch_type, status);

                create index merch_requests_employee_id_status_idx
                    on merch_requests (employee_id, status);
                "
            );
        }

        public override void Down()
        {
            Execute.Sql("drop table if exists merch_requests;");
        }
    }
}