-- Add IsDetailImage column to ProductImages table
-- This script will add the missing column to fix the error

-- Check if column exists first
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'ProductImages' 
               AND COLUMN_NAME = 'IsDetailImage')
BEGIN
    ALTER TABLE ProductImages 
    ADD IsDetailImage bit NOT NULL DEFAULT 0;
    
    PRINT 'IsDetailImage column added successfully to ProductImages table';
END
ELSE
BEGIN
    PRINT 'IsDetailImage column already exists in ProductImages table';
END
