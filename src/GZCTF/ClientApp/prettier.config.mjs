/** @type {import("prettier").Config} */
export default {
  useTabs: false,
  tabWidth: 2,
  singleQuote: true,
  trailingComma: 'es5',
  semi: false,
  printWidth: 120,
  htmlWhitespaceSensitivity: 'ignore',
  jsonRecursiveSort: true,
  plugins: ['@trivago/prettier-plugin-sort-imports', 'prettier-plugin-sort-json'],
  importOrder: [
    '<THIRD_PARTY_MODULES>',
    '^@Components/(.*)$',
    '^@Utils/(.*)$',
    '^@Hooks/(.*)$',
    '^@Api$',
    '^@Styles/(.*)$',
    '^@(.*).css$',
    '^[./]',
  ],
}
