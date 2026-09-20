import { Center, Loader, Text } from '@mantine/core'
import { useParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { RouteOutline } from '@Components/learning/RouteOutline'
import { WithNavBar } from '@Components/WithNavbar'
import { useLearningPathPreview } from '@Hooks/useLearning'
import { usePageTitle } from '@Hooks/usePageTitle'
import { useLanguage } from '@Utils/I18n'

const LearningRoute = () => {
  const { slug } = useParams()
  const { locale } = useLanguage()
  const { t } = useTranslation('learning')
  const { data: path, error } = useLearningPathPreview(slug, locale)
  usePageTitle(path?.title ?? t('title'))

  return (
    <WithNavBar minWidth={0}>
      {!path && !error ? <Center><Loader /></Center> : error ? <Text c="red">{t('loadFailed')}</Text> : <RouteOutline path={path!} />}
    </WithNavBar>
  )
}

export default LearningRoute

