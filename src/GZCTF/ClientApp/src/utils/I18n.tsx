import { useLocalStorage } from '@mantine/hooks'
import dayjs from 'dayjs'
import 'dayjs/locale/ja'
import 'dayjs/locale/zh'
import localizedFormat from 'dayjs/plugin/localizedFormat'
import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import resources from 'virtual:i18next-loader'

dayjs.extend(localizedFormat)

export const LanguageMap = {
  zh_CN: '简体中文',
  en_US: 'English',
  ja_JP: '日本語',
}

export const defaultLanguage = 'en_US'
export let apiLanguage: string = defaultLanguage

export type SupportedLanguages = keyof typeof LanguageMap

export const useLanguage = () => {
  const { i18n } = useTranslation()

  const [language, setLanguageInner] = useLocalStorage({
    key: 'language',
    defaultValue: i18n.language,
    getInitialValueInEffect: false,
  })

  useEffect(() => {
    i18n.changeLanguage(language)
    dayjs.locale(language)
    apiLanguage = language.replace('_', '-')
    document.documentElement.setAttribute('lang', language.replace('_', '-').toLowerCase())
  }, [language])

  const supportedLanguages = Object.keys(resources) as SupportedLanguages[]

  const setLanguage = (lang: SupportedLanguages) => {
    // check if language is supported
    if (supportedLanguages.includes(lang)) {
      setLanguageInner(lang)
    } else {
      console.warn(`Language ${lang} is not supported, fallback to ${defaultLanguage}`)
      setLanguageInner(defaultLanguage)
    }
  }

  const locale = language.split('_')[0]

  return { language, locale, setLanguage, supportedLanguages }
}

export const normalizeLanguage = (language: string) => language.toUpperCase().replace(/[_-].*/, '')

export const convertLanguage = (language: string): SupportedLanguages => {
  const normalizedLanguage = normalizeLanguage(language)

  const matchedLanguage = Object.keys(LanguageMap).filter(
    (lang) => normalizeLanguage(lang) === normalizedLanguage
  )
  if (matchedLanguage.length > 0) {
    return matchedLanguage.at(0) as SupportedLanguages
  }

  return defaultLanguage
}
