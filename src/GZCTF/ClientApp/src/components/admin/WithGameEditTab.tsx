import { Button, Group, GroupProps, LoadingOverlay, Stack, Tabs } from '@mantine/core'
import {
  mdiAccountGroupOutline,
  mdiBullhornOutline,
  mdiClockOutline,
  mdiFileDocumentCheckOutline,
  mdiFlagVariantOutline,
  mdiFlagOutline,
  mdiHeartPulse,
  mdiKeyboardBackspace,
  mdiSync,
  mdiTagOutline,
  mdiTextBoxOutline,
  mdiCommentTextOutline,
  mdiAccountKey,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, Link, useNavigate, useParams } from 'react-router'
import { useUser } from '@Hooks/useUser'
import { Role } from '@Api'
import { AdminPage } from '@Components/admin/AdminPage'
import { DEFAULT_LOADING_OVERLAY } from '@Utils/Shared'
import misc from '@Styles/Misc.module.css'

export interface GameEditTabProps extends React.PropsWithChildren {
  head?: React.ReactNode
  headProps?: GroupProps
  contentPos?: React.CSSProperties['justifyContent']
  isLoading?: boolean
  backUrl?: string
}

export const WithGameEditTab: FC<GameEditTabProps> = ({
  children,
  isLoading,
  contentPos,
  head,
  backUrl,
  ...others
}) => {
  const navigate = useNavigate()
  const location = useLocation()
  const { id } = useParams()
  const { t } = useTranslation()
  const { user } = useUser()
  const isAdmin = user?.role === Role.Admin

  const pages = [
    { icon: mdiAccountKey, title: t('admin.tab.games.managers', 'Managers'), path: 'managers', adminOnly: true },
    { icon: mdiTextBoxOutline, title: t('admin.tab.games.info'), path: 'info' },
    { icon: mdiBullhornOutline, title: t('admin.tab.games.notices'), path: 'notices' },
    // 'pending' must precede 'challenges' so the fuzzy path.includes match
    // resolves /challenges/pending to this tab instead of plain Challenges.
    { icon: mdiClockOutline, title: t('admin.tab.games.pending', 'Pending'), path: 'pending' },
    { icon: mdiSync, title: t('admin.tab.games.watches', 'Watches'), path: 'watches' },
    { icon: mdiFlagOutline, title: t('admin.tab.games.challenges'), path: 'challenges' },
    { icon: mdiTagOutline, title: t('admin.tab.games.divisions'), path: 'divisions' },
    { icon: mdiAccountGroupOutline, title: t('admin.tab.games.review'), path: 'review' },
    { icon: mdiCommentTextOutline, title: t('admin.title.challenge_reviews', 'Reviews'), path: 'challengereviews' },
    { icon: mdiFileDocumentCheckOutline, title: t('admin.tab.games.writeups'), path: 'writeups' },
    { icon: mdiHeartPulse, title: t('admin.tab.games.health', 'Health'), path: 'health' },
    { icon: mdiFlagVariantOutline, title: t('admin.tab.games.flag_egress', 'Flag Egress'), path: 'flagegress' },
  ].filter((p) => isAdmin || !p.adminOnly)

  const getTab = (path: string) => pages.find((page) => path.includes(page.path))

  const [activeTab, setActiveTab] = useState(getTab(location.pathname)?.path ?? pages[0].path)

  useEffect(() => {
    const tab = getTab(location.pathname)
    if (tab) {
      setActiveTab(tab.path ?? '')
    } else if (pages.length > 0) {
      navigate(`/admin/games/${id}/${pages[0].path}`)
    }
  }, [location, pages, id, navigate])

  return (
    <AdminPage
      {...others}
      head={
        <>
          <Button
            w="10rem"
            component={Link}
            classNames={{ inner: misc.justifyBetween }}
            leftSection={<Icon path={mdiKeyboardBackspace} size={1} />}
            to={backUrl ?? '/admin/games'}
          >
            {t('admin.button.back')}
          </Button>
          <Group wrap="nowrap" justify={contentPos ?? 'space-between'} w="calc(100% - 11rem)">
            {head}
          </Group>
        </>
      }
    >
      <Group wrap="nowrap" justify="space-between" align="flex-start" w="100%" pb="xl">
        <Tabs
          orientation="vertical"
          value={activeTab}
          onChange={(value) => value && navigate(`/admin/games/${id}/${value}`)}
          classNames={{
            root: misc.w10rem,
            list: misc.w10rem,
          }}
        >
          <Tabs.List>
            {pages.map((page) => (
              <Tabs.Tab key={page.path} leftSection={<Icon path={page.icon} size={1} />} value={page.path}>
                {page.title}
              </Tabs.Tab>
            ))}
          </Tabs.List>
        </Tabs>
        <Stack w="calc(100% - 11rem)" pos="relative">
          <LoadingOverlay visible={isLoading ?? false} overlayProps={DEFAULT_LOADING_OVERLAY} />
          {children}
        </Stack>
      </Group>
    </AdminPage>
  )
}
