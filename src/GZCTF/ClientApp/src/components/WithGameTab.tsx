import { Card, LoadingOverlay, Stack, Text, Title } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiChartLine, mdiExclamationThick, mdiFlagOutline, mdiMonitorEye } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import duration from 'dayjs/plugin/duration'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import GameProgress from '@Components/GameProgress'
import IconTabs from '@Components/IconTabs'
import { RequireRole } from '@Components/WithRole'
import { DEFAULT_LOADING_OVERLAY } from '@Utils/Shared'
import { getGameStatus, useGame } from '@Utils/useGame'
import { usePageTitle } from '@Utils/usePageTitle'
import { useUserRole } from '@Utils/useUser'
import { DetailedGameInfoModel, ParticipationStatus, Role } from '@Api'

dayjs.extend(duration)

const GameCountdown: FC<{ game?: DetailedGameInfoModel }> = ({ game }) => {
  const { endTime, progress } = getGameStatus(game)

  const [now, setNow] = useState(dayjs())

  const { t } = useTranslation()

  useEffect(() => {
    if (!game || dayjs() > dayjs(game.end)) return
    const interval = setInterval(() => setNow(dayjs()), 1000)
    return () => clearInterval(interval)
  }, [game])

  const countdown = dayjs.duration(endTime.diff(now))

  return (
    <Card
      miw="9rem"
      ta="center"
      pt={4}
      style={{
        overflow: 'visible',
      }}
    >
      <Text fw="bold" lineClamp={1}>
        {countdown.asHours() > 999
          ? t('game.content.game_lasts_long')
          : countdown.asSeconds() > 0
            ? `${Math.floor(countdown.asHours())} : ${countdown.format('mm : ss')}`
            : t('game.content.game_ended')}
      </Text>
      <Card.Section mt={4}>
        <GameProgress percentage={progress} py={0} />
      </Card.Section>
    </Card>
  )
}

const WithGameTab: FC<React.PropsWithChildren> = ({ children }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const location = useLocation()
  const navigate = useNavigate()

  const { role } = useUserRole()
  const { game, status } = useGame(numId)
  const { t } = useTranslation()

  const finished = dayjs() > dayjs(game?.end ?? new Date())

  const pages = [
    {
      icon: mdiFlagOutline,
      title: t('game.tab.challenge'),
      path: 'challenges',
      link: 'challenges',
      requireJoin: true,
      requireRole: Role.User,
    },
    {
      icon: mdiChartLine,
      title: t('game.tab.scoreboard'),
      path: 'scoreboard',
      link: 'scoreboard',
      requireJoin: false,
      requireRole: Role.User,
    },
    {
      icon: mdiMonitorEye,
      title: t('game.tab.monitor.index'),
      path: 'monitor',
      link: 'monitor/events',
      requireJoin: false,
      requireRole: Role.Monitor,
    },
  ]

  const filteredPages = pages
    .filter((p) => RequireRole(p.requireRole, role))
    .filter((p) => !p.requireJoin || game?.status === ParticipationStatus.Accepted)
    .filter((p) => !p.requireJoin || !finished || game?.practiceMode)

  const tabs = filteredPages.map((p) => ({
    tabKey: p.link,
    label: p.title,
    icon: <Icon path={p.icon} size={1} />,
  }))
  const getTab = (path: string) => filteredPages?.findIndex((page) => path.includes(page.path))

  const tabIndex = getTab(location.pathname)
  const [activeTab, setActiveTab] = useState(tabIndex < 0 ? 0 : tabIndex)

  const onChange = (active: number, tabKey: string) => {
    setActiveTab(active)
    navigate(`/games/${numId}/${tabKey}`)
  }

  usePageTitle(game?.title)

  useEffect(() => {
    const tab = getTab(location.pathname)
    if (!tab || tab < 0) return
    setActiveTab(tab)
  })

  useEffect(() => {
    if (game) {
      const now = dayjs()
      if (now < dayjs(game.start)) {
        navigate(`/games/${numId}`)
        showNotification({
          id: 'no-access',
          color: 'yellow',
          message: t('game.notification.not_started'),
          icon: <Icon path={mdiExclamationThick} size={1} />,
        })
      } else if (
        !location.pathname.includes('scoreboard') &&
        status === ParticipationStatus.Suspended &&
        now < dayjs(game.end)
      ) {
        navigate(`/games/${numId}`)
        showNotification({
          id: 'no-access',
          color: 'yellow',
          message: t('game.notification.suspended'),
          icon: <Icon path={mdiExclamationThick} size={1} />,
        })
      } else if (
        !location.pathname.includes('scoreboard') &&
        !game.practiceMode &&
        now > dayjs(game.end) &&
        !RequireRole(Role.Monitor, role)
      ) {
        navigate(`/games/${numId}`)
        showNotification({
          id: 'no-access',
          color: 'yellow',
          message: t('game.notification.ended'),
          icon: <Icon path={mdiExclamationThick} size={1} />,
        })
      }
    }
  }, [game, status, role, location])

  return (
    <Stack pos="relative" mt="md">
      <LoadingOverlay visible={!game} overlayProps={DEFAULT_LOADING_OVERLAY} />
      <IconTabs
        active={activeTab}
        onTabChange={onChange}
        tabs={tabs}
        aside={
          game && (
            <>
              <Title>{game?.title}</Title>
              <GameCountdown game={game} />
            </>
          )
        }
      />
      {children}
    </Stack>
  )
}

export default WithGameTab
