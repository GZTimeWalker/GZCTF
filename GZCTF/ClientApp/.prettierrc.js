module.exports = {
  "singleQuote": true,
  "printWidth": 100,
  "htmlWhitespaceSensitivity": "ignore",
  "plugins": [require.resolve("@trivago/prettier-plugin-sort-imports")],
  "importOrder": ["^next/(.*)$", "^@mantine/(.*)$", "^@mdi/(.*)$", "^[./]"]
}
