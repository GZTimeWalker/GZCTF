module.exports = {
  useTabs: false,
  tabWidth: 2,
  singleQuote: true,
  trailingComma: 'es5',
  semi: false,
  printWidth: 100,
  htmlWhitespaceSensitivity: 'ignore',
  plugins: [require.resolve('@trivago/prettier-plugin-sort-imports')],
  importOrder: [
    '^react(.*)$',
    '^@mantine/(.*)$',
    '^@mdi/(.*)$',
    '^@Components/(.*)$',
    '^@Utils/(.*)$',
    '^@Api$',
    '^[./]',
  ],
}
