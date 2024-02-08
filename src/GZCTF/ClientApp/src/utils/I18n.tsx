import { useLocalStorage } from '@mantine/hooks'
import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import resources from 'virtual:i18next-loader'

export const LanguageMap = {
  zh_CN: '简体中文',
  en_US: 'English',
  ja_JP: '日本語',
}

export type SupportedLanguages = keyof typeof LanguageMap

export const useLanguage = () => {
  const [language, setLanguageInner] = useLocalStorage({
    key: 'language',
    defaultValue: 'zh_CN',
    getInitialValueInEffect: false,
  })

  const { i18n } = useTranslation()

  useEffect(() => {
    i18n.changeLanguage(language)
    apiLanguage = language.replace('_', '-')
  }, [language])

  const supportedLanguages = Object.keys(resources) as SupportedLanguages[]

  const setLanguage = (lang: SupportedLanguages) => {
    // check if language is supported
    if (supportedLanguages.includes(lang)) {
      setLanguageInner(lang)
    } else {
      console.warn(`Language ${lang} is not supported, fallback to zh_CN`)
      setLanguageInner('zh_CN')
    }
  }

  return { language, setLanguage, supportedLanguages }
}

export let apiLanguage: string = 'zh-CN'
