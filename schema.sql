CREATE TABLE "Customers" (
    "CustomerId" INTEGER NOT NULL CONSTRAINT "PK_Customers" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "ContactNumber" TEXT NOT NULL,
    "Email" TEXT NULL,
    "Address" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "LastTransaction" TEXT NULL
);


CREATE TABLE "Inventories" (
    "InventoryId" INTEGER NOT NULL CONSTRAINT "PK_Inventories" PRIMARY KEY AUTOINCREMENT,
    "ItemName" TEXT NOT NULL,
    "CurrentStock" TEXT NOT NULL,
    "Unit" TEXT NOT NULL,
    "MinimumThreshold" TEXT NOT NULL,
    "UsualRestockAmount" TEXT NULL,
    "UnitCost" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    "LastRestockedAt" TEXT NULL
);


CREATE TABLE "Users" (
    "UserId" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "Username" TEXT NOT NULL,
    "Password" TEXT NOT NULL,
    "SecurityQuestion" TEXT NOT NULL,
    "SecurityAnswer" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL
);


CREATE TABLE "Transactions" (
    "TransactionId" INTEGER NOT NULL CONSTRAINT "PK_Transactions" PRIMARY KEY AUTOINCREMENT,
    "CustomerId" INTEGER NOT NULL,
    "TotalCost" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "FulfillmentType" TEXT NOT NULL,
    "PaymentType" TEXT NOT NULL,
    "AmountPaid" TEXT NULL,
    "WashStatus" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "CompletedAt" TEXT NULL,
    CONSTRAINT "FK_Transactions_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("CustomerId") ON DELETE RESTRICT
);


CREATE TABLE "InventoryRestockHistories" (
    "RestockId" INTEGER NOT NULL CONSTRAINT "PK_InventoryRestockHistories" PRIMARY KEY AUTOINCREMENT,
    "InventoryId" INTEGER NOT NULL,
    "QuantityChange" TEXT NOT NULL,
    "RestockDate" TEXT NOT NULL,
    "Notes" TEXT NULL,
    CONSTRAINT "FK_InventoryRestockHistories_Inventories_InventoryId" FOREIGN KEY ("InventoryId") REFERENCES "Inventories" ("InventoryId") ON DELETE RESTRICT
);


CREATE TABLE "Services" (
    "ServiceId" INTEGER NOT NULL CONSTRAINT "PK_Services" PRIMARY KEY AUTOINCREMENT,
    "ServiceName" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "PricePerKilo" TEXT NULL,
    "MinKilo" TEXT NULL,
    "MinKiloCharge" TEXT NULL,
    "ExcessPerKilo" TEXT NULL,
    "FlatRate" TEXT NULL,
    "DetergentItemId" INTEGER NULL,
    "DetergentUsage" TEXT NULL,
    "ConditionerItemId" INTEGER NULL,
    "ConditionerUsage" TEXT NULL,
    "OtherItemId" INTEGER NULL,
    "OtherUsage" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Services_Inventories_ConditionerItemId" FOREIGN KEY ("ConditionerItemId") REFERENCES "Inventories" ("InventoryId") ON DELETE SET NULL,
    CONSTRAINT "FK_Services_Inventories_DetergentItemId" FOREIGN KEY ("DetergentItemId") REFERENCES "Inventories" ("InventoryId") ON DELETE SET NULL,
    CONSTRAINT "FK_Services_Inventories_OtherItemId" FOREIGN KEY ("OtherItemId") REFERENCES "Inventories" ("InventoryId") ON DELETE SET NULL
);


CREATE TABLE "InventoryUsageHistories" (
    "UsageId" INTEGER NOT NULL CONSTRAINT "PK_InventoryUsageHistories" PRIMARY KEY AUTOINCREMENT,
    "InventoryId" INTEGER NOT NULL,
    "QuantityUsed" TEXT NOT NULL,
    "UsageDate" TEXT NOT NULL,
    "TransactionId" INTEGER NULL,
    "Notes" TEXT NULL,
    CONSTRAINT "FK_InventoryUsageHistories_Inventories_InventoryId" FOREIGN KEY ("InventoryId") REFERENCES "Inventories" ("InventoryId") ON DELETE RESTRICT,
    CONSTRAINT "FK_InventoryUsageHistories_Transactions_TransactionId" FOREIGN KEY ("TransactionId") REFERENCES "Transactions" ("TransactionId") ON DELETE RESTRICT
);


CREATE TABLE "TransactionItems" (
    "TransactionItemId" INTEGER NOT NULL CONSTRAINT "PK_TransactionItems" PRIMARY KEY AUTOINCREMENT,
    "TransactionId" INTEGER NOT NULL,
    "ServiceId" INTEGER NOT NULL,
    "WeightKg" TEXT NOT NULL,
    "LineCost" TEXT NOT NULL,
    CONSTRAINT "FK_TransactionItems_Services_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "Services" ("ServiceId") ON DELETE RESTRICT,
    CONSTRAINT "FK_TransactionItems_Transactions_TransactionId" FOREIGN KEY ("TransactionId") REFERENCES "Transactions" ("TransactionId") ON DELETE CASCADE
);


CREATE INDEX "IX_InventoryRestockHistories_InventoryId" ON "InventoryRestockHistories" ("InventoryId");


CREATE INDEX "IX_InventoryUsageHistories_InventoryId" ON "InventoryUsageHistories" ("InventoryId");


CREATE INDEX "IX_InventoryUsageHistories_TransactionId" ON "InventoryUsageHistories" ("TransactionId");


CREATE INDEX "IX_Services_ConditionerItemId" ON "Services" ("ConditionerItemId");


CREATE INDEX "IX_Services_DetergentItemId" ON "Services" ("DetergentItemId");


CREATE INDEX "IX_Services_OtherItemId" ON "Services" ("OtherItemId");


CREATE INDEX "IX_TransactionItems_ServiceId" ON "TransactionItems" ("ServiceId");


CREATE INDEX "IX_TransactionItems_TransactionId" ON "TransactionItems" ("TransactionId");


CREATE INDEX "IX_Transactions_CustomerId" ON "Transactions" ("CustomerId");


