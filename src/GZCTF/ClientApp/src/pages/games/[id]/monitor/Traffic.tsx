import {
  ActionIcon,
  Center,
  Divider,
  Grid,
  Group,
  Paper,
  rem,
  Stack,
  Text,
  Title,
  Tooltip,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiClose, mdiDownloadMultiple } from '@mdi/js'
import Icon from '@mdi/react'
import dayjs from 'dayjs'
import { CSSProperties, FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import ScrollSelect from '@Components/ScrollSelect'
import { ChallengeItem, FileItem, TeamItem } from '@Components/TrafficItems'
import WithGameMonitorTab from '@Components/WithGameMonitor'
import { useTooltipStyles } from '@Utils/ThemeOverride'
import api, { FileRecord } from '@Api'

const SWROptions = {
  refreshInterval: 0,
  shouldRetryOnError: false,
  revalidateOnFocus: false,
}

const Traffic: FC = () => {
  const { id } = useParams()
  const gameId = parseInt(id ?? '-1')

  const [challengeId, setChallengeId] = useState<number | null>(null)
  const [participationId, setParticipationId] = useState<number | null>(null)
  const { classes: tooltipClasses, theme } = useTooltipStyles()

  const { t } = useTranslation()

  const { data: challengeTraffic } = api.game.useGameGetChallengesWithTrafficCapturing(
    gameId,
    SWROptions
  )
  const { data: teamTraffic } = api.game.useGameGetChallengeTraffic(
    challengeId ?? 0,
    SWROptions,
    !!challengeId
  )
  const { data: fileRecords } = api.game.useGameGetTeamTrafficAll(
    challengeId ?? 0,
    participationId ?? 0,
    SWROptions,
    !!challengeId && !!participationId
  )

  const onDownload = (item: FileRecord) => {
    if (!challengeId || !participationId || !item.fileName) return

    window.open(`/api/game/captures/${challengeId}/${participationId}/${item.fileName}`, '_blank')
  }

  const onDownloadAll = () => {
    if (!challengeId || !participationId) {
      showNotification({
        color: 'red',
        title: t('common.error.encountered'),
        message: t('game.notification.select_chal_and_part'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    window.open(`/api/game/captures/${challengeId}/${participationId}/all`, '_blank')
  }

  const orderedFileRecords =
    fileRecords?.sort((a, b) => dayjs(b.updateTime).diff(dayjs(a.updateTime))) ?? []

  const innerStyle: CSSProperties = {
    borderRight: `${rem(2)} solid ${
      theme.colorScheme === 'dark' ? theme.colors.dark[4] : theme.colors.gray[4]
    }`,
  }

  const srollHeight = 'calc(100vh - 174px)'
  const headerHeight = rem(32)

  return (
    <WithGameMonitorTab>
      {!challengeTraffic || challengeTraffic?.length === 0 ? (
        <Center h="calc(100vh - 140px)">
          <Stack spacing={0}>
            <Title order={2}>{t('game.content.no_traffic.title')}</Title>
            <Text>{t('game.content.no_traffic.comment')}</Text>
          </Stack>
        </Center>
      ) : (
        <Paper shadow="md" p="md">
          <Grid gutter={0} h="calc(100vh - 142px)" grow>
            <Grid.Col span={3} style={innerStyle}>
              <Group h={headerHeight} pb="3px" px="xs">
                <Text size="md" weight={700}>
                  {t('common.label.challenge')}
                </Text>
              </Group>
              <Divider size="sm" />
              <ScrollSelect
                itemComponent={ChallengeItem}
                items={challengeTraffic}
                selectedId={challengeId}
                onSelectId={setChallengeId}
                h={srollHeight}
              />
            </Grid.Col>
            <Grid.Col span={3} style={innerStyle}>
              <Group h={headerHeight} pb="3px" px="xs">
                <Text size="md" weight={700}>
                  {t('common.label.team')}
                </Text>
              </Group>
              <Divider size="sm" />
              <ScrollSelect
                itemComponent={TeamItem}
                items={teamTraffic}
                selectedId={participationId}
                onSelectId={setParticipationId}
                h={srollHeight}
              />
            </Grid.Col>
            <Grid.Col span={6}>
              <Group h={headerHeight} pb="3px" px="xs" position="apart">
                <Text size="md" weight={700}>
                  {t('game.label.traffic')}
                </Text>
                <Tooltip
                  label={t('game.button.download_all_traffic')}
                  position="left"
                  classNames={tooltipClasses}
                >
                  <ActionIcon size="md" onClick={onDownloadAll}>
                    <Icon path={mdiDownloadMultiple} size={1} />
                  </ActionIcon>
                </Tooltip>
              </Group>
              <Divider size="sm" />
              <ScrollSelect
                itemComponent={FileItem}
                items={orderedFileRecords}
                customClick
                onSelectId={onDownload}
                h={srollHeight}
              />
            </Grid.Col>
          </Grid>
        </Paper>
      )}
    </WithGameMonitorTab>
  )
}

export default Traffic
