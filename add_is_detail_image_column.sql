-- Add IsDetailImage column to ProductImages table
ALTER TABLE ProductImages 
ADD IsDetailImage bit NOT NULL DEFAULT 0;
