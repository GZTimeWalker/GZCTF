// 文档：https://github.com/IFreeOvO/i18n-cli/tree/master/packages/i18n-extract-cli
// 先在 Git 中暂存现有更改，然后运行 pnpx @ifreeovo/i18n-extract-cli -c i18n.config.js

const path = require('node:path')

module.exports = {
    // rules每个属性对应的是不同后缀文件的处理方式
    rules: {
        // js: {
        //     caller: '', // 自定义this.$t('xxx')中的this。不填则默认没有调用对象
        //     functionName: 't', // 自定义this.$t('xxx')中的$t
        //     customizeKey: function (key, currentFilePath) {
        //         return key
        //     }, // 自定义this.$t('xxx')中的'xxx'部分的生成规则
        //     importDeclaration: 'import { t } from "i18n"', // 默认在文件里导入i18n包。不填则默认不导入i18n的包。由于i18n的npm包有很多，用户可根据项目自行修改导入语法
        //     forceImport: false, // 即使文件没出现中文，也强行插入importDeclaration定义的语句
        // },
        // ts,cjs,mjs,jsx,tsx配置方式同上
        tsx: {
            caller: '',
            functionName: 't',
            customizeKey: function (key, currentFilePath) {
                return key
                // return path.basename(currentFilePath) + '_' + key
            },
            importDeclaration: '',
            functionSnippets: '',
            forceImport: false,
        },
    },
    globalRule: {
        ignoreMethods: [] // 忽略指定函数调用的中文提取。例如想忽略sensor.track('中文')的提取。这里就写['sensor.track']
    },
    // prettier配置，参考https://prettier.io/docs/en/options.html
    prettier: {
        useTabs: false,
        tabWidth: 2,
        singleQuote: true,
        trailingComma: 'es5',
        semi: false,
        printWidth: 100,
        htmlWhitespaceSensitivity: 'ignore',
        plugins: ['@trivago/prettier-plugin-sort-imports'],
        importOrder: [
            '<THIRD_PARTY_MODULES>',
            '^@Components/(.*)$',
            '^@Utils/(.*)$',
            '^@Api$',
            '^[./]',
        ],
    },
    skipExtract: false, // 跳过提取中文阶段
    skipTranslate: true, // 跳过翻译语言包阶段。默认不翻译
}