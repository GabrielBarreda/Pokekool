import re, sys

def patch(path, old, new, count=1):
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    occ = content.count(old)
    assert occ == count, f"{path}: expected {count} occurrence(s) of block, found {occ}\n---OLD---\n{old}"
    content = content.replace(old, new)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"OK: {path}")

base = "src/app/features"

# 1) market.models.ts -> MarketCardDto
patch(f"{base}/bourse/market.models.ts",
"""export interface MarketCardDto {
  id: string;
  tcgId: string;
  number: string;
  name: string;
  supertype: string | null;""",
"""export interface MarketCardDto {
  id: string;
  tcgId: string;
  number: string;
  name: string;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName: string | null;
  supertype: string | null;""")

# 2) boutique.models.ts -> ShopProductDto, ShopStockDto
patch(f"{base}/boutique/boutique.models.ts",
"""export interface ShopProductDto {
  id: string;
  shopId: string;
  type: ShopProductType;
  cardId: string | null;
  cardTcgId: string | null;
  cardName: string | null;
  cardImage: string | null;""",
"""export interface ShopProductDto {
  id: string;
  shopId: string;
  type: ShopProductType;
  cardId: string | null;
  cardTcgId: string | null;
  cardName: string | null;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName: string | null;
  cardImage: string | null;""")

patch(f"{base}/boutique/boutique.models.ts",
"""export interface ShopStockDto {
  id: string;
  shopId: string;
  cardId: string | null;
  cardTcgId: string | null;
  cardName: string | null;
  cardImage: string | null;""",
"""export interface ShopStockDto {
  id: string;
  shopId: string;
  cardId: string | null;
  cardTcgId: string | null;
  cardName: string | null;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName: string | null;
  cardImage: string | null;""")

# 3) client.models.ts -> ShopProductDto (optional fields pattern)
patch(f"{base}/client/client.models.ts",
"""export interface ShopProductDto {
  id: string;
  shopId: string;
  type: string;
  cardId?: string | null;
  cardTcgId?: string | null;
  cardName?: string | null;
  cardImage?: string | null;""",
"""export interface ShopProductDto {
  id: string;
  shopId: string;
  type: string;
  cardId?: string | null;
  cardTcgId?: string | null;
  cardName?: string | null;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName?: string | null;
  cardImage?: string | null;""")

# 4) collection.models.ts -> CollectionCardDto, CardScanMatchDto
patch(f"{base}/collection/collection.models.ts",
"""export interface CollectionCardDto {
  id: string;
  tcgId: string;
  number: string;
  name: string;
  supertype: string | null;""",
"""export interface CollectionCardDto {
  id: string;
  tcgId: string;
  number: string;
  name: string;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName: string | null;
  supertype: string | null;""")

patch(f"{base}/collection/collection.models.ts",
"""export interface CardScanMatchDto {
  cardId: string | null;
  cardName: string | null;
  setName: string | null;""",
"""export interface CardScanMatchDto {
  cardId: string | null;
  cardName: string | null;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName: string | null;
  setName: string | null;""")

# 5) dashboard.models.ts -> RecentlyScrapedCardDto, ForSaleCardDto
patch(f"{base}/dashboard/dashboard.models.ts",
"""export interface RecentlyScrapedCardDto {
  cardId:        string;
  tcgId:         string | null;
  name:          string;
  imageSmall:    string | null;""",
"""export interface RecentlyScrapedCardDto {
  cardId:        string;
  tcgId:         string | null;
  name:          string;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName:   string | null;
  imageSmall:    string | null;""")

patch(f"{base}/dashboard/dashboard.models.ts",
"""export interface ForSaleCardDto {
  cardId:          string;
  tcgId:           string | null;
  name:            string;
  imageSmall:      string | null;""",
"""export interface ForSaleCardDto {
  cardId:          string;
  tcgId:           string | null;
  name:            string;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName:     string | null;
  imageSmall:      string | null;""")

# 6) storage.models.ts -> StorageCardDetailDto, StorageBinderCardDto, ShopStockDto, CardScanMatchDto
patch(f"{base}/boutique/stockage/storage.models.ts",
"""export interface StorageCardDetailDto {
  cardId: string | null;
  tcgId: string | null;
  name: string;
  setName: string | null;""",
"""export interface StorageCardDetailDto {
  cardId: string | null;
  tcgId: string | null;
  name: string;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName: string | null;
  setName: string | null;""")

patch(f"{base}/boutique/stockage/storage.models.ts",
"""export interface StorageBinderCardDto {
  locationId: string;
  ownerType: string;
  ownerId: string;
  cardId: string | null;
  tcgId: string | null;
  name: string;
  imageSmall: string | null;""",
"""export interface StorageBinderCardDto {
  locationId: string;
  ownerType: string;
  ownerId: string;
  cardId: string | null;
  tcgId: string | null;
  name: string;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName: string | null;
  imageSmall: string | null;""")

patch(f"{base}/boutique/stockage/storage.models.ts",
"""export interface ShopStockDto {
  id: string;
  shopId: string;
  cardId: string | null;
  cardTcgId: string | null;
  cardName: string | null;
  cardImage: string | null;""",
"""export interface ShopStockDto {
  id: string;
  shopId: string;
  cardId: string | null;
  cardTcgId: string | null;
  cardName: string | null;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName: string | null;
  cardImage: string | null;""")

patch(f"{base}/boutique/stockage/storage.models.ts",
"""export interface CardScanMatchDto {
  cardId: string | null;
  cardTcgId: string | null;
  cardName: string | null;
  setName: string | null;""",
"""export interface CardScanMatchDto {
  cardId: string | null;
  cardTcgId: string | null;
  cardName: string | null;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName: string | null;
  setName: string | null;""")

print("ALL DONE")
