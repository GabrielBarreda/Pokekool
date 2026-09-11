// Fichier vidé (2026-09-09) : doublon d'un IDesignTimeDbContextFactory<PokekoolDbContext> avec
// Persistence/PokekoolDbContextFactory.cs (la version active, qui gère UseVector() pour
// pgvector — nécessaire depuis l'ajout du matching visuel DINOv2, voir son commentaire XML pour
// le détail). Les deux factories présentes simultanément dans le même projet faisaient échouer
// la découverte design-time d'EF Core ("An item with the same key has already been added: 
// PokekoolDbContext") lors de tout `dotnet ef migrations add`/`database update`. Vestige non
// supprimé (pas de permission de suppression de fichier accordée pour ce répertoire) — voir
// Pokekool.Infrastructure/Persistence/PokekoolDbContextFactory.cs pour la factory active.
