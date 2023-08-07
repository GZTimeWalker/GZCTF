import dayjs from 'dayjs'
import duration from 'dayjs/plugin/duration'
import React, { FC, useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { Card, Stack, Title, Text, LoadingOverlay, useMantineTheme } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiFlagOutline, mdiMonitorEye, mdiChartLine, mdiExclamationThick } from '@mdi/js'
import { Icon } from '@mdi/react'
import CustomProgress from '@Components/CustomProgress'
import IconTabs from '@Components/IconTabs'
import { RequireRole } from '@Components/WithRole'
import { getGameStatus, useGame } from '@Utils/useGame'
import { usePageTitle } from '@Utils/usePageTitle'
import { useUserRole } from '@Utils/useUser'
import { DetailedGameInfoModel, ParticipationStatus, Role } from '@Api'

const pages = [
  {
    icon: mdiFlagOutline,
    title: '比赛题目',
    path: 'challenges',
    link: 'challenges',
    color: 'blue',
    requireJoin: true,
    requireRole: Role.User,
  },
  {
    icon: mdiChartLine,
    title: '积分总榜',
    path: 'scoreboard',
    link: 'scoreboard',
    color: 'yellow',
    requireJoin: false,
    requireRole: Role.User,
  },
  {
    icon: mdiMonitorEye,
    title: '比赛监控',
    path: 'monitor',
    link: 'monitor/events',
    color: 'green',
    requireJoin: false,
    requireRole: Role.Monitor,
  },
]

dayjs.extend(duration)

const GameCountdown: FC<{ game?: DetailedGameInfoModel }> = ({ game }) => {
  const { endTime, progress } = getGameStatus(game)

  const [now, setNow] = useState(dayjs())

  useEffect(() => {
    if (!game || dayjs() > dayjs(game.end)) return
    const interval = setInterval(() => setNow(dayjs()), 1000)
    return () => clearInterval(interval)
  }, [game])

  const countdown = dayjs.duration(endTime.diff(now))

  return (
    <Card
      w="9rem"
      ta="center"
      pt={4}
      style={{
        overflow: 'visible',
      }}
    >
      <Text fw={700} lineClamp={1}>
        {countdown.asHours() > 999
          ? '比赛还会很久'
          : countdown.asSeconds() > 0
          ? `${Math.floor(countdown.asHours())} : ${countdown.format('mm : ss')}`
          : '比赛已结束'}
      </Text>
      <Card.Section mt={4}>
        <CustomProgress percentage={progress} py={0} />
      </Card.Section>
    </Card>
  )
}

const WithGameTab: FC<React.PropsWithChildren> = ({ children }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const location = useLocation()
  const navigate = useNavigate()

  const theme = useMantineTheme()
  const { role } = useUserRole()
  const { game, status } = useGame(numId)

  const finished = dayjs() > dayjs(game?.end ?? new Date())
  const filteredPages = pages
    .filter((p) => RequireRole(p.requireRole, role))
    .filter((p) => !p.requireJoin || game?.status === ParticipationStatus.Accepted)
    .filter((p) => !p.requireJoin || !finished || game?.practiceMode)

  const tabs = filteredPages.map((p) => ({
    tabKey: p.link,
    label: p.title,
    icon: <Icon path={p.icon} size={1} />,
    color: p.color,
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
          message: '比赛尚未开始',
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
          message: '您已被禁赛',
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
          message: '比赛已经结束',
          icon: <Icon path={mdiExclamationThick} size={1} />,
        })
      }
    }
  }, [game, status, role, location])

  return (
    <Stack pos="relative" mt="md">
      <LoadingOverlay
        visible={!game}
        overlayOpacity={1}
        overlayColor={theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.white[2]}
      />
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
