// Ambient type declarations for @creepjs/* path-alias modules.
// The actual source lives in src/lib/creepjs (a git submodule) and is
// resolved at bundle time via the vite alias. TypeScript only needs to
// know the shape; typing everything as `any` avoids TS6 strict-mode
// errors in third-party code we don't own.
declare module '@creepjs/*';
