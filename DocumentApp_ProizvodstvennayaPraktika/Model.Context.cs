﻿

//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------


namespace DocumentApp_ProizvodstvennayaPraktika
{

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;


public partial class Entities : DbContext
{
    public Entities()
        : base("name=Entities")
    {

    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        throw new UnintentionalCodeFirstException();
    }


    public virtual DbSet<ClientContracts> ClientContracts { get; set; }

    public virtual DbSet<ClientDetails> ClientDetails { get; set; }

    public virtual DbSet<ContractCategories> ContractCategories { get; set; }

    public virtual DbSet<ContractHistory> ContractHistory { get; set; }

    public virtual DbSet<ContractTemplates> ContractTemplates { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<TemplateFields> TemplateFields { get; set; }

    public virtual DbSet<Users> Users { get; set; }

}

}

