using Xunit;

// Colecciones de test en SERIE (sin paralelismo entre colecciones).
//
// El porqué: cada clase de integración monta un host (TestApp) cuyo AddHangfire
// pisa el JobStorage.Current global con un Hangfire.InMemory nuevo, y al
// desbotear el fixture ese storage se DESECHA (el restore del global en
// TestApp.Dispose solo repone la referencia DESPUÉS de que base.Dispose ya lo
// mató). xUnit corre las colecciones en paralelo, así que un Enqueue de un
// test unitario (los servicios encolan emails por Hangfire) puede agarrar el
// Current del host —o un snapshot tomado antes— en el instante en que se
// desecha: BackgroundJobClientException → ObjectDisposedException. El storage
// de TestDatabase no se desecha nunca y el de un host vivo no puede explotar:
// el dispuesto solo puede ser el de un host muriendo, y eso solo pasa con
// colecciones en paralelo. Con las colecciones en serie el ciclo de vida de
// ningún host pisa una lectura/escritura del global.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
