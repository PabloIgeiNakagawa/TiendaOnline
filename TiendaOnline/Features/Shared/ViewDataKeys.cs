namespace TiendaOnline.Features.Shared
{
    /// <summary>
    /// Únicos keys válidos de ViewData en la app. Un nuevo caso se suma acá como constante:
    /// un literal escrito a mano compila y si lleva un typo el layout nunca lo renderiza
    /// (la página pierde su título en silencio).
    /// El test de shape de ReglaDeTitulosDeVista guarda esta regla.
    /// </summary>
    public static class ViewDataKeys
    {
        /// <summary>Título de página (Title) — lo declara la vista o el action que la sirve; lo renderiza el layout.</summary>
        public const string Title = "Title";
    }
}
