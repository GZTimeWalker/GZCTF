import { Stack, Text, Title } from '@mantine/core'
import { FC, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router-dom'
import WithNavBar from '@Components/WithNavbar'
import Icon404 from '@Components/icon/404Icon'
import { usePageTitle } from '@Utils/usePageTitle'
import classes from '@Styles/Placeholder.module.css'

const Error404: FC = () => {
  const navigate = useNavigate()
  const location = useLocation()

  const { t } = useTranslation()

  usePageTitle(t('common.title.404'))

  useEffect(() => {
    if (location.pathname !== '/404') {
      navigate('/404')
    }
  }, [location])

  return (
    <WithNavBar minWidth={0}>
      <Stack gap={0} className={classes.board}>
        <Icon404 />
        <Title order={1}>{t('common.content.404.title')}</Title>
        <Text fw="bold">{t('common.content.404.text')}</Text>
      </Stack>
    </WithNavBar>
  )
}

export default Error404
