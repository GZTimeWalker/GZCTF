import { useLocalStorage } from '@mantine/hooks'
import i18n from 'i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import { useEffect } from 'react'
import { initReactI18next } from 'react-i18next'
import resources from 'virtual:i18next-loader'

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: 'zh_CN',
    interpolation: {
      escapeValue: false,
    },
    detection: {
      convertDetectedLanguage: 'Iso15897',
    },
  })

export const useLanguage = () => {
  const [language, setLanguageInner] = useLocalStorage({
    key: 'language',
    defaultValue: 'zh_CN',
  })

  useEffect(() => {
    i18n.changeLanguage(language)
  }, [language])

  const supportedLanguages = Object.keys(resources)

  const setLanguage = (lang: string) => {
    // check if language is supported
    if (supportedLanguages.includes(lang)) {
      setLanguageInner(lang)
    } else {
      setLanguageInner('zh_CN')
    }
  }

  return { language, setLanguage, supportedLanguages }
}

export default i18n
