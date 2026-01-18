-- Add Azerbaijani Categories and Subcategories to SmartTeam Database
-- Run this script directly on your database

-- 1. Ticarət avadanlıqları (Trade Equipment)
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, CreatedAt) 
VALUES (NEWID(), N'Ticarət avadanlıqları', 'ticaret-avadanliqlari', N'Ticarət üçün lazım olan avadanlıqlar', 1, 1, GETUTCDATE());

-- Get the ID of the main category for subcategories
DECLARE @TicaretId UNIQUEIDENTIFIER = (SELECT Id FROM Categories WHERE Name = N'Ticarət avadanlıqları');

-- Subcategories for Ticarət avadanlıqları
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, ParentCategoryId, CreatedAt) VALUES
(NEWID(), N'POS Komputerlər', 'pos-komputerler', N'POS sistemlər üçün komputerlər', 1, 1, @TicaretId, GETUTCDATE()),
(NEWID(), N'Çek priterlər', 'cek-printerler', N'Çek çap edən priterlər', 1, 2, @TicaretId, GETUTCDATE()),
(NEWID(), N'Barkod printerlər', 'barkod-printerler', N'Barkod çap edən priterlər', 1, 3, @TicaretId, GETUTCDATE()),
(NEWID(), N'Mini printerlər', 'mini-printerler', N'Kiçik ölçülü priterlər', 1, 4, @TicaretId, GETUTCDATE()),
(NEWID(), N'Barkod scanerlər', 'barkod-scanerler', N'Barkod oxuyan cihazlar', 1, 5, @TicaretId, GETUTCDATE()),
(NEWID(), N'Tərəzilər', 'tereziler', N'Çəki ölçən tərəzilər', 1, 6, @TicaretId, GETUTCDATE()),
(NEWID(), N'Pul yeşikləri', 'pul-yesikleri', N'Pul saxlama yeşikləri', 1, 7, @TicaretId, GETUTCDATE()),
(NEWID(), N'Çek və Barkod kağızları', 'cek-ve-barkod-kagizlari', N'Çek və barkod üçün kağızlar', 1, 8, @TicaretId, GETUTCDATE());

-- 2. Kompüterlər (Computers)
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, CreatedAt) 
VALUES (NEWID(), N'Kompüterlər', 'komputerler', N'Müxtəlif növ kompüterlər', 1, 2, GETUTCDATE());

DECLARE @KomputerlerId UNIQUEIDENTIFIER = (SELECT Id FROM Categories WHERE Name = N'Kompüterlər');

-- Subcategories for Kompüterlər
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, ParentCategoryId, CreatedAt) VALUES
(NEWID(), N'Ofis Kompüterləri', 'ofis-komputerleri', N'Ofis işləri üçün kompüterlər', 1, 1, @KomputerlerId, GETUTCDATE()),
(NEWID(), N'Oyun və Dizayn Kompüterləri', 'oyun-ve-dizayn-komputerleri', N'Oyun və dizayn üçün güclü kompüterlər', 1, 2, @KomputerlerId, GETUTCDATE()),
(NEWID(), N'Monoboklar', 'monoboklar', N'Bir hissədə kompüterlər', 1, 3, @KomputerlerId, GETUTCDATE()),
(NEWID(), N'Mini Kompüterləri', 'mini-komputerleri', N'Kiçik ölçülü kompüterlər', 1, 4, @KomputerlerId, GETUTCDATE());

-- 3. Noutbuklar (Laptops)
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, CreatedAt) 
VALUES (NEWID(), N'Noutbuklar', 'noutbuklar', N'Müxtəlif növ noutbuklar', 1, 3, GETUTCDATE());

DECLARE @NoutbuklarId UNIQUEIDENTIFIER = (SELECT Id FROM Categories WHERE Name = N'Noutbuklar');

-- Subcategories for Noutbuklar
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, ParentCategoryId, CreatedAt) VALUES
(NEWID(), N'Ofis Noutbukları', 'ofis-noutbuklari', N'Ofis işləri üçün noutbuklar', 1, 1, @NoutbuklarId, GETUTCDATE()),
(NEWID(), N'Oyun Noutbukları', 'oyun-noutbuklari', N'Oyun üçün güclü noutbuklar', 1, 2, @NoutbuklarId, GETUTCDATE()),
(NEWID(), N'Planşet tipli', 'planset-tipli', N'Planşet tipli noutbuklar', 1, 3, @NoutbuklarId, GETUTCDATE());

-- 4. Müşahidə sistemləri (Surveillance Systems)
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, CreatedAt) 
VALUES (NEWID(), N'Müşahidə sistemləri', 'musahide-sistemleri', N'Təhlükəsizlik və müşahidə sistemləri', 1, 4, GETUTCDATE());

DECLARE @MusahideId UNIQUEIDENTIFIER = (SELECT Id FROM Categories WHERE Name = N'Müşahidə sistemləri');

-- Subcategories for Müşahidə sistemləri
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, ParentCategoryId, CreatedAt) VALUES
(NEWID(), N'Analoq Kamera sistemləri', 'analoq-kamera-sistemleri', N'Analoq kamera sistemləri', 1, 1, @MusahideId, GETUTCDATE()),
(NEWID(), N'İP Kamera sistemləri', 'ip-kamera-sistemleri', N'İP kamera sistemləri', 1, 2, @MusahideId, GETUTCDATE()),
(NEWID(), N'WIFI Kameraları', 'wifi-kameralari', N'WIFI kameralar', 1, 3, @MusahideId, GETUTCDATE()),
(NEWID(), N'Yaddaş Qurğuları', 'yaddas-qurgulari', N'Yaddaş qurğuları', 1, 4, @MusahideId, GETUTCDATE()),
(NEWID(), N'Damafonlar', 'damafonlar', N'Damafon sistemləri', 1, 5, @MusahideId, GETUTCDATE()),
(NEWID(), N'Access Control', 'access-control', N'Giriş nəzarət sistemləri', 1, 6, @MusahideId, GETUTCDATE());

-- Get IDs for deeper subcategories
DECLARE @AnaloqId UNIQUEIDENTIFIER = (SELECT Id FROM Categories WHERE Name = N'Analoq Kamera sistemləri' AND ParentCategoryId = @MusahideId);
DECLARE @IPId UNIQUEIDENTIFIER = (SELECT Id FROM Categories WHERE Name = N'İP Kamera sistemləri' AND ParentCategoryId = @MusahideId);
DECLARE @YaddasId UNIQUEIDENTIFIER = (SELECT Id FROM Categories WHERE Name = N'Yaddaş Qurğuları' AND ParentCategoryId = @MusahideId);

-- Sub-subcategories for Analoq Kamera sistemləri
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, ParentCategoryId, CreatedAt) VALUES
(NEWID(), N'Kamera', 'kamera', N'Analoq kameralar', 1, 1, @AnaloqId, GETUTCDATE()),
(NEWID(), N'DVR', 'dvr', N'DVR qurğuları', 1, 2, @AnaloqId, GETUTCDATE());

-- Sub-subcategories for İP Kamera sistemləri
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, ParentCategoryId, CreatedAt) VALUES
(NEWID(), N'Kamera', 'ip-kamera', N'İP kameralar', 1, 1, @IPId, GETUTCDATE()),
(NEWID(), N'NVR', 'nvr', N'NVR qurğuları', 1, 2, @IPId, GETUTCDATE());

-- Sub-subcategories for Yaddaş Qurğuları
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, ParentCategoryId, CreatedAt) VALUES
(NEWID(), N'HDD', 'hdd', N'Hard disk sürücüləri', 1, 1, @YaddasId, GETUTCDATE()),
(NEWID(), N'Mikro SD', 'mikro-sd', N'Mikro SD kartlar', 1, 2, @YaddasId, GETUTCDATE());

-- 5. Kompüter avadanlıqları (Computer Equipment)
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, CreatedAt) 
VALUES (NEWID(), N'Kompüter avadanlıqları', 'komputer-avadanliqlari', N'Kompüter üçün avadanlıqlar', 1, 5, GETUTCDATE());

DECLARE @KomputerAvadanliqlariId UNIQUEIDENTIFIER = (SELECT Id FROM Categories WHERE Name = N'Kompüter avadanlıqları');

-- Subcategories for Kompüter avadanlıqları
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, ParentCategoryId, CreatedAt) VALUES
(NEWID(), N'Monitor', 'monitor', N'Kompüter monitorları', 1, 1, @KomputerAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'SSD', 'ssd', N'SSD sürücüləri', 1, 2, @KomputerAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'HDD', 'hdd-avadanliq', N'Hard disk sürücüləri', 1, 3, @KomputerAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'RAM', 'ram', N'RAM yaddaşları', 1, 4, @KomputerAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'CPU', 'cpu', N'Prosessorlar', 1, 5, @KomputerAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'Case', 'case', N'Kompüter qutuları', 1, 6, @KomputerAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'Qida Bloku', 'qida-bloku', N'Qida blokları', 1, 7, @KomputerAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'Qulaqlıq', 'qulaqliq', N'Qulaqlıqlar', 1, 8, @KomputerAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'Klavyatura', 'klavyatura', N'Klavyaturalar', 1, 9, @KomputerAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'Maus', 'maus', N'Mauslar', 1, 10, @KomputerAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'Dinamik', 'dinamik', N'Dinamiklər', 1, 11, @KomputerAvadanliqlariId, GETUTCDATE());

-- 6. Ofis avadanlıqları (Office Equipment)
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, CreatedAt) 
VALUES (NEWID(), N'Ofis avadanlıqları', 'ofis-avadanliqlari', N'Ofis üçün avadanlıqlar', 1, 6, GETUTCDATE());

DECLARE @OfisAvadanliqlariId UNIQUEIDENTIFIER = (SELECT Id FROM Categories WHERE Name = N'Ofis avadanlıqları');

-- Subcategories for Ofis avadanlıqları
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, ParentCategoryId, CreatedAt) VALUES
(NEWID(), N'UPS', 'ups', N'UPS sistemləri', 1, 1, @OfisAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'Printer', 'printer', N'Priterlər', 1, 2, @OfisAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'Uzadıcı', 'uzadici', N'Uzadıcılar', 1, 3, @OfisAvadanliqlariId, GETUTCDATE());

-- 7. Şəbəkə avadanlıqları (Network Equipment)
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, CreatedAt) 
VALUES (NEWID(), N'Şəbəkə avadanlıqları', 'sebeke-avadanliqlari', N'Şəbəkə üçün avadanlıqlar', 1, 7, GETUTCDATE());

DECLARE @SebekeAvadanliqlariId UNIQUEIDENTIFIER = (SELECT Id FROM Categories WHERE Name = N'Şəbəkə avadanlıqları');

-- Subcategories for Şəbəkə avadanlıqları
INSERT INTO Categories (Id, Name, Slug, Description, IsActive, SortOrder, ParentCategoryId, CreatedAt) VALUES
(NEWID(), N'Router', 'router', N'Routerlər', 1, 1, @SebekeAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'Access point', 'access-point', N'Access pointlər', 1, 2, @SebekeAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'Range extender', 'range-extender', N'Range extenderlər', 1, 3, @SebekeAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'Switch', 'switch', N'Switchlər', 1, 4, @SebekeAvadanliqlariId, GETUTCDATE()),
(NEWID(), N'Wifi adapter', 'wifi-adapter', N'Wifi adapterlər', 1, 5, @SebekeAvadanliqlariId, GETUTCDATE());

PRINT 'Azerbaijani categories and subcategories have been successfully added to the database!';
