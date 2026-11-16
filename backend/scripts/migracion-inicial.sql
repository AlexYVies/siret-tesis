IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AspNetRoles] (
    [Id] int NOT NULL IDENTITY,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] int NOT NULL IDENTITY,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TiposArticulo] (
    [IdTipoArticulo] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [Descripcion] nvarchar(500) NULL,
    [Activo] bit NOT NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    CONSTRAINT [PK_TiposArticulo] PRIMARY KEY ([IdTipoArticulo])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Alumnos] (
    [IdAlumno] int NOT NULL IDENTITY,
    [IdUsuario] int NOT NULL,
    [Nombre] nvarchar(150) NOT NULL,
    [NumeroControl] nvarchar(30) NOT NULL,
    [Correo] nvarchar(320) NOT NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    CONSTRAINT [PK_Alumnos] PRIMARY KEY ([IdAlumno]),
    CONSTRAINT [FK_Alumnos_AspNetUsers_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    [UsuarioId] int NULL,
    [RolId] int NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RolId] FOREIGN KEY ([RolId]) REFERENCES [AspNetRoles] ([Id]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [AspNetUsers] ([Id])
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] int NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Profesores] (
    [IdProfesor] int NOT NULL IDENTITY,
    [IdUsuario] int NULL,
    [Nombre] nvarchar(150) NOT NULL,
    [Activo] bit NOT NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    CONSTRAINT [PK_Profesores] PRIMARY KEY ([IdProfesor]),
    CONSTRAINT [FK_Profesores_AspNetUsers_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ProfesorPosgrado] (
    [IdProfesorPosgrado] int NOT NULL IDENTITY,
    [IdProfesor] int NOT NULL,
    [Nivel] int NOT NULL,
    [Programa] nvarchar(150) NOT NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    CONSTRAINT [PK_ProfesorPosgrado] PRIMARY KEY ([IdProfesorPosgrado]),
    CONSTRAINT [FK_ProfesorPosgrado_Profesores_IdProfesor] FOREIGN KEY ([IdProfesor]) REFERENCES [Profesores] ([IdProfesor]) ON DELETE CASCADE
);
GO

CREATE TABLE [Tesis] (
    [Folio] uniqueidentifier NOT NULL,
    [IdAlumno] int NOT NULL,
    [Titulo] nvarchar(300) NOT NULL,
    [Nivel] int NOT NULL,
    [Programa] nvarchar(150) NOT NULL,
    [IdAsesor] int NOT NULL,
    [TieneCoAsesor] bit NOT NULL,
    [IdCoAsesor] int NULL,
    [Version] int NOT NULL,
    [Estado] int NOT NULL,
    [FechaExamenGrado] datetime2 NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    CONSTRAINT [PK_Tesis] PRIMARY KEY ([Folio]),
    CONSTRAINT [FK_Tesis_Alumnos_IdAlumno] FOREIGN KEY ([IdAlumno]) REFERENCES [Alumnos] ([IdAlumno]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tesis_Profesores_IdAsesor] FOREIGN KEY ([IdAsesor]) REFERENCES [Profesores] ([IdProfesor]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tesis_Profesores_IdCoAsesor] FOREIGN KEY ([IdCoAsesor]) REFERENCES [Profesores] ([IdProfesor]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Articulos] (
    [IdArticulo] int NOT NULL IDENTITY,
    [Folio] uniqueidentifier NOT NULL,
    [Nombre] nvarchar(300) NOT NULL,
    [Url] nvarchar(500) NOT NULL,
    [Doi] nvarchar(200) NULL,
    [IdTipoArticulo] int NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    CONSTRAINT [PK_Articulos] PRIMARY KEY ([IdArticulo]),
    CONSTRAINT [FK_Articulos_Tesis_Folio] FOREIGN KEY ([Folio]) REFERENCES [Tesis] ([Folio]) ON DELETE CASCADE,
    CONSTRAINT [FK_Articulos_TiposArticulo_IdTipoArticulo] FOREIGN KEY ([IdTipoArticulo]) REFERENCES [TiposArticulo] ([IdTipoArticulo]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ConfirmacionesAsesoria] (
    [IdConfirmacion] int NOT NULL IDENTITY,
    [Folio] uniqueidentifier NOT NULL,
    [IdProfesor] int NOT NULL,
    [Tipo] int NOT NULL,
    [Estado] int NOT NULL,
    [Fecha] datetime2 NOT NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    CONSTRAINT [PK_ConfirmacionesAsesoria] PRIMARY KEY ([IdConfirmacion]),
    CONSTRAINT [FK_ConfirmacionesAsesoria_Profesores_IdProfesor] FOREIGN KEY ([IdProfesor]) REFERENCES [Profesores] ([IdProfesor]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ConfirmacionesAsesoria_Tesis_Folio] FOREIGN KEY ([Folio]) REFERENCES [Tesis] ([Folio]) ON DELETE CASCADE
);
GO

CREATE TABLE [Dictamenes] (
    [IdDictamen] int NOT NULL IDENTITY,
    [Folio] uniqueidentifier NOT NULL,
    [IdDirector] int NOT NULL,
    [Resultado] int NOT NULL,
    [Observaciones] nvarchar(1000) NULL,
    [Fecha] datetime2 NOT NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    CONSTRAINT [PK_Dictamenes] PRIMARY KEY ([IdDictamen]),
    CONSTRAINT [FK_Dictamenes_Tesis_Folio] FOREIGN KEY ([Folio]) REFERENCES [Tesis] ([Folio]) ON DELETE CASCADE
);
GO

CREATE TABLE [DocumentosTesis] (
    [IdDocumento] int NOT NULL IDENTITY,
    [Folio] uniqueidentifier NOT NULL,
    [Version] int NOT NULL,
    [Nombre] nvarchar(255) NOT NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    CONSTRAINT [PK_DocumentosTesis] PRIMARY KEY ([IdDocumento]),
    CONSTRAINT [FK_DocumentosTesis_Tesis_Folio] FOREIGN KEY ([Folio]) REFERENCES [Tesis] ([Folio]) ON DELETE CASCADE
);
GO

CREATE TABLE [Favoritos] (
    [IdFavorito] int NOT NULL IDENTITY,
    [IdUsuario] int NOT NULL,
    [Folio] uniqueidentifier NOT NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    CONSTRAINT [PK_Favoritos] PRIMARY KEY ([IdFavorito]),
    CONSTRAINT [FK_Favoritos_AspNetUsers_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Favoritos_Tesis_Folio] FOREIGN KEY ([Folio]) REFERENCES [Tesis] ([Folio]) ON DELETE CASCADE
);
GO

CREATE TABLE [RevisionesAsesor] (
    [IdRevisionAsesor] int NOT NULL IDENTITY,
    [Folio] uniqueidentifier NOT NULL,
    [IdProfesor] int NOT NULL,
    [Version] int NOT NULL,
    [EsUltimaVersion] bit NOT NULL,
    [Decision] int NOT NULL,
    [Observaciones] nvarchar(1000) NULL,
    [Fecha] datetime2 NOT NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    CONSTRAINT [PK_RevisionesAsesor] PRIMARY KEY ([IdRevisionAsesor]),
    CONSTRAINT [FK_RevisionesAsesor_Profesores_IdProfesor] FOREIGN KEY ([IdProfesor]) REFERENCES [Profesores] ([IdProfesor]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RevisionesAsesor_Tesis_Folio] FOREIGN KEY ([Folio]) REFERENCES [Tesis] ([Folio]) ON DELETE CASCADE
);
GO

CREATE TABLE [RevisionesIA] (
    [IdRevision] int NOT NULL IDENTITY,
    [Folio] uniqueidentifier NOT NULL,
    [Version] int NOT NULL,
    [Resultado] int NOT NULL,
    [Porcentaje] decimal(5,2) NOT NULL,
    [Fecha] datetime2 NOT NULL,
    [FechaReg] datetime2 NOT NULL,
    [FechaMod] datetime2 NOT NULL,
    CONSTRAINT [PK_RevisionesIA] PRIMARY KEY ([IdRevision]),
    CONSTRAINT [FK_RevisionesIA_Tesis_Folio] FOREIGN KEY ([Folio]) REFERENCES [Tesis] ([Folio]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_Alumnos_IdUsuario] ON [Alumnos] ([IdUsuario]);
GO

CREATE INDEX [IX_Articulos_Folio] ON [Articulos] ([Folio]);
GO

CREATE INDEX [IX_Articulos_IdTipoArticulo] ON [Articulos] ([IdTipoArticulo]);
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RolId] ON [AspNetUserRoles] ([RolId]);
GO

CREATE INDEX [IX_AspNetUserRoles_UsuarioId] ON [AspNetUserRoles] ([UsuarioId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE INDEX [IX_ConfirmacionesAsesoria_Folio] ON [ConfirmacionesAsesoria] ([Folio]);
GO

CREATE INDEX [IX_ConfirmacionesAsesoria_IdProfesor] ON [ConfirmacionesAsesoria] ([IdProfesor]);
GO

CREATE INDEX [IX_Dictamenes_Folio] ON [Dictamenes] ([Folio]);
GO

CREATE INDEX [IX_DocumentosTesis_Folio] ON [DocumentosTesis] ([Folio]);
GO

CREATE INDEX [IX_Favoritos_Folio] ON [Favoritos] ([Folio]);
GO

CREATE INDEX [IX_Favoritos_IdUsuario] ON [Favoritos] ([IdUsuario]);
GO

CREATE UNIQUE INDEX [IX_Profesores_IdUsuario] ON [Profesores] ([IdUsuario]) WHERE [IdUsuario] IS NOT NULL;
GO

CREATE INDEX [IX_ProfesorPosgrado_IdProfesor] ON [ProfesorPosgrado] ([IdProfesor]);
GO

CREATE INDEX [IX_RevisionesAsesor_Folio] ON [RevisionesAsesor] ([Folio]);
GO

CREATE INDEX [IX_RevisionesAsesor_IdProfesor] ON [RevisionesAsesor] ([IdProfesor]);
GO

CREATE INDEX [IX_RevisionesIA_Folio] ON [RevisionesIA] ([Folio]);
GO

CREATE INDEX [IX_Tesis_IdAlumno] ON [Tesis] ([IdAlumno]);
GO

CREATE INDEX [IX_Tesis_IdAsesor] ON [Tesis] ([IdAsesor]);
GO

CREATE INDEX [IX_Tesis_IdCoAsesor] ON [Tesis] ([IdCoAsesor]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260908155943_Inicial', N'8.0.30');
GO

COMMIT;
GO

