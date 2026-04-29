namespace CIVARCH
{
    /// <summary>
    /// Trida AppConfig slouzi k definici a uchovani nastaveni aplikace. Aplikaci je potreba restartovat po zmene nektereho nastaveni.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Toto nastaveni umoznuje zobrazit sloupec ID v tabulce.
        /// Pokud je hodnota 'true' tak se sloupec zobrazuje.
        /// 
        /// Default: true
        /// </summary>
        public const bool TableDisplayId = true;

        /// <summary>
        /// Toto nastaveni povoluje hromadny export.
        /// Pokud je hodnota 'true' tak je hromadny export povolen.
        /// 
        /// Default: true
        /// </summary>
        public const bool EnableHromadnyExport = true;

        /// <summary>
        /// Toto nastaveni definuje pocet radku v tabulce zaznamu.
        /// 
        /// Default: 50
        /// </summary>
        public const int TableRowsCount = 50;

        /// <summary>
        /// Toto nastaveni definuje nazev aplikace.
        /// 
        /// Default: CIVARCH
        /// </summary>
        public const string AppName = "CIVARCH";

        /// <summary>
        /// Toto nastaveni urcuje zda bude cast s ovladacimi prvky nad tabulkou (vyhledavani, strankovani) vybarvena nebo ne.
        /// 
        /// Default: true
        /// </summary>
        public const bool DisplayColoredTableHeader = true;
    }
}
