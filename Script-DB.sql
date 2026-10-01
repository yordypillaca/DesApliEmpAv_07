USE master;
GO

IF DB_ID(N'BibliotecaDB') IS NOT NULL
BEGIN
    ALTER DATABASE BibliotecaDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE BibliotecaDB;
END
GO

CREATE DATABASE BibliotecaDB;
GO

USE BibliotecaDB;
GO

CREATE TABLE Autores (
    AutorId       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre        NVARCHAR(100)     NOT NULL,
    Nacionalidad  NVARCHAR(60)      NOT NULL,
    Activo        BIT               NOT NULL CONSTRAINT DF_Autores_Activo DEFAULT 1
);
GO

CREATE TABLE Libros (
    LibroId     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Titulo      NVARCHAR(200)     NOT NULL,
    ISBN        NVARCHAR(20)      NOT NULL,
    AutorId     INT               NOT NULL,
    Ejemplares  INT               NOT NULL,
    Activo      BIT               NOT NULL CONSTRAINT DF_Libros_Activo DEFAULT 1,
    CONSTRAINT UQ_Libros_ISBN UNIQUE (ISBN),
    CONSTRAINT FK_Libros_Autores FOREIGN KEY (AutorId) REFERENCES Autores (AutorId),
    CONSTRAINT CK_Libros_Ejemplares CHECK (Ejemplares >= 0)
);
GO

CREATE TABLE Socios (
    SocioId  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DNI      NVARCHAR(8)       NOT NULL,
    Nombre   NVARCHAR(120)     NOT NULL,
    Email    NVARCHAR(120)     NULL,
    Activo   BIT               NOT NULL CONSTRAINT DF_Socios_Activo DEFAULT 1,
    CONSTRAINT UQ_Socios_DNI UNIQUE (DNI)
);
GO

CREATE TABLE Prestamos (
    PrestamoId     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SocioId        INT               NOT NULL,
    FechaPrestamo  DATE              NOT NULL,
    FechaLimite    DATE              NOT NULL,
    Estado         NVARCHAR(20)      NOT NULL,
    CONSTRAINT FK_Prestamos_Socios FOREIGN KEY (SocioId) REFERENCES Socios (SocioId),
    CONSTRAINT CK_Prestamos_Fechas CHECK (FechaLimite >= FechaPrestamo),
    CONSTRAINT CK_Prestamos_Estado CHECK (Estado IN (N'Pendiente', N'Devuelto'))
);
GO

CREATE TABLE DetallePrestamo (
    PrestamoId       INT  NOT NULL,
    LibroId          INT  NOT NULL,
    FechaDevolucion  DATE NULL,
    CONSTRAINT PK_DetallePrestamo PRIMARY KEY (PrestamoId, LibroId),
    CONSTRAINT FK_Detalle_Prestamos FOREIGN KEY (PrestamoId) REFERENCES Prestamos (PrestamoId),
    CONSTRAINT FK_Detalle_Libros FOREIGN KEY (LibroId) REFERENCES Libros (LibroId)
);
GO

INSERT INTO Autores (Nombre, Nacionalidad) VALUES
    (N'Mario Vargas Llosa', N'Perú'),
    (N'Gabriel García Márquez', N'Colombia'),
    (N'Julio Cortázar', N'Argentina'),
    (N'Isabel Allende', N'Chile'),
    (N'Jorge Luis Borges', N'Argentina'),
    (N'César Vallejo', N'Perú'),
    (N'Laura Esquivel', N'México'),
    (N'Pablo Neruda', N'Chile');
GO

INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares) VALUES
    (N'La ciudad y los perros', N'9780000000011', 1, 4),
    (N'Conversación en La Catedral', N'9780000000012', 1, 3),
    (N'La fiesta del chivo', N'9780000000013', 1, 2),
    (N'Cien años de soledad', N'9780000000014', 2, 4),
    (N'El amor en los tiempos del cólera', N'9780000000015', 2, 3),
    (N'Crónica de una muerte anunciada', N'9780000000016', 2, 2),
    (N'Rayuela', N'9780000000017', 3, 3),
    (N'Bestiario', N'9780000000018', 3, 2),
    (N'La casa de los espíritus', N'9780000000019', 4, 3),
    (N'De amor y de sombra', N'9780000000020', 4, 2),
    (N'Ficciones', N'9780000000021', 5, 2),
    (N'El Aleph', N'9780000000022', 5, 2),
    (N'Los heraldos negros', N'9780000000023', 6, 2),
    (N'Trilce', N'9780000000024', 6, 1),
    (N'Como agua para chocolate', N'9780000000025', 7, 3),
    (N'La ley del amor', N'9780000000026', 7, 2),
    (N'Veinte poemas de amor y una canción desesperada', N'9780000000027', 8, 4),
    (N'Canto general', N'9780000000028', 8, 2),
    (N'Confieso que he vivido', N'9780000000029', 8, 1),
    (N'El coronel no tiene quien le escriba', N'9780000000030', 2, 0);
GO

INSERT INTO Socios (DNI, Nombre, Email) VALUES
    (N'45678901', N'Ana Torres', N'ana.torres@mail.com'),
    (N'45678902', N'Luis Ramos', N'luis.ramos@mail.com'),
    (N'45678903', N'Carla Vega', N'carla.vega@mail.com'),
    (N'45678904', N'Pedro Quispe', N'pedro.quispe@mail.com'),
    (N'45678905', N'María Huamán', N'maria.huaman@mail.com'),
    (N'45678906', N'José Díaz', N'jose.diaz@mail.com'),
    (N'45678907', N'Lucía Mendoza', N'lucia.mendoza@mail.com'),
    (N'45678908', N'Andrés Paredes', N'andres.paredes@mail.com'),
    (N'45678909', N'Sofía Rojas', N'sofia.rojas@mail.com'),
    (N'45678910', N'Diego Castro', NULL);
GO

DECLARE @Hoy DATE = CAST(GETDATE() AS DATE);

INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) VALUES
    (1, DATEADD(DAY, -20, @Hoy), DATEADD(DAY, -13, @Hoy), N'Devuelto'),
    (2, DATEADD(DAY,  -5, @Hoy), DATEADD(DAY,   2, @Hoy), N'Pendiente'),
    (3, DATEADD(DAY, -10, @Hoy), DATEADD(DAY,  -3, @Hoy), N'Pendiente'),
    (4, DATEADD(DAY,  -2, @Hoy), DATEADD(DAY,   5, @Hoy), N'Pendiente'),
    (5, DATEADD(DAY,  -1, @Hoy), DATEADD(DAY,   6, @Hoy), N'Pendiente');

INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
    (1,  1, DATEADD(DAY, -14, @Hoy)),
    (2,  4, NULL),
    (3,  7, DATEADD(DAY, -4, @Hoy)),
    (3,  9, NULL),
    (4, 15, NULL),
    (5, 11, NULL),
    (5, 13, NULL),
    (5, 17, NULL);
GO
