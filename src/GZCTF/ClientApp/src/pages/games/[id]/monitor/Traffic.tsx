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
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiDeleteForeverOutline, mdiDownloadMultiple } from '@mdi/js'
import Icon from '@mdi/react'
import dayjs from 'dayjs'
import { CSSProperties, FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { ScrollSelect } from '@Components/ScrollSelect'
import { ChallengeItem, FileItem, TeamItem } from '@Components/TrafficItems'
import { WithGameMonitor } from '@Components/WithGameMonitor'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useLanguage } from '@Utils/I18n'
import { HunamizeSize } from '@Utils/Shared'
import api, { FileRecord } from '@Api'
import tooltipClasses from '@Styles/Tooltip.module.css'

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
  const [disabled, setDisabled] = useState(false)
  const theme = useMantineTheme()

  const { t } = useTranslation()
  const { locale } = useLanguage()
  const { colorScheme } = useMantineColorScheme()
  const modals = useModals()

  const { data: challengeTraffic, mutate: mutateChallenges } = api.game.useGameGetChallengesWithTrafficCapturing(
    gameId,
    SWROptions
  )
  const { data: teamTraffic, mutate: mutateTeams } = api.game.useGameGetChallengeTraffic(
    challengeId ?? 0,
    SWROptions,
    !!challengeId
  )
  const { data: fileRecords, mutate: mutateTraffic } = api.game.useGameGetTeamTrafficAll(
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

  const onDelete = async (item: FileRecord) => {
    if (!challengeId || !participationId || !item.fileName) return

    setDisabled(true)

    try {
      await api.game.gameDeleteTeamTraffic(challengeId, participationId, item.fileName)
      showNotification({
        color: 'teal',
        message: t('game.notification.traffic.deleted'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (e) {
      showErrorNotification(e, t)
    } finally {
      mutateTeams()
      mutateTraffic()
      setDisabled(false)
    }
  }

  const onDeleteAll = async () => {
    if (!challengeId || !participationId) return

    setDisabled(true)

    try {
      await api.game.gameDeleteAllTeamTraffic(challengeId, participationId)
      showNotification({
        color: 'teal',
        message: t('game.notification.traffic.deleted'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (e) {
      showErrorNotification(e, t)
    } finally {
      mutateTraffic()
      mutateTeams()
      mutateChallenges()
      setDisabled(false)
    }
  }

  const totalFileSize = fileRecords?.reduce((acc, cur) => acc + (cur?.size ?? 0), 0) ?? 0

  const orderedFileRecords = fileRecords?.sort((a, b) => dayjs(b.updateTime).diff(dayjs(a.updateTime))) ?? []

  const innerStyle: CSSProperties = {
    borderRight: `${rem(2)} solid ${colorScheme === 'dark' ? theme.colors.dark[4] : theme.colors.gray[4]}`,
  }

  const srollHeight = 'calc(100vh - 174px)'
  const headerHeight = rem(32)

  challengeTraffic?.sort((a, b) => a.category?.localeCompare(b.category ?? '') ?? 0)
  teamTraffic?.sort((a, b) => (a.teamId ?? 0) - (b.teamId ?? 0))

  return (
    <WithGameMonitor isLoading={!challengeTraffic}>
      {!challengeTraffic || challengeTraffic?.length === 0 ? (
        <Center h="calc(100vh - 140px)">
          <Stack gap={0}>
            <Title order={2}>{t('game.content.no_traffic.title')}</Title>
            <Text>{t('game.content.no_traffic.comment')}</Text>
          </Stack>
        </Center>
      ) : (
        <Paper shadow="md" p="md">
          <Grid gutter={0} h="calc(100vh - 142px)">
            <Grid.Col span={3} style={innerStyle}>
              <Group h={headerHeight} pb="3px" px="xs">
                <Text size="md" fw="bold">
                  {t('common.label.challenge')}
                </Text>
              </Group>
              <Divider size="sm" />
              <ScrollSelect
                itemComponent={ChallengeItem}
                items={challengeTraffic}
                selectedId={challengeId}
                onSelect={setChallengeId}
                h={srollHeight}
              />
            </Grid.Col>
            <Grid.Col span={3} style={innerStyle}>
              <Group h={headerHeight} pb="3px" px="xs">
                <Text size="md" fw="bold">
                  {t('common.label.team')}
                </Text>
              </Group>
              <Divider size="sm" />
              <ScrollSelect
                itemComponent={TeamItem}
                items={teamTraffic}
                selectedId={participationId}
                onSelect={setParticipationId}
                h={srollHeight}
              />
            </Grid.Col>
            <Grid.Col span={6}>
              <Group h={headerHeight} pb="3px" px="xs" justify="space-between" wrap="nowrap">
                <Text size="md" fw="bold">
                  {t('game.label.traffic')}
                  <Text span px="md" fw="bold" size="sm" c="dimmed">
                    {HunamizeSize(totalFileSize ?? 0)}
                  </Text>
                </Text>
                <Group justify="right" gap="sm" wrap="nowrap">
                  <Tooltip label={t('game.button.delete.all_traffic')} position="left" classNames={tooltipClasses}>
                    <ActionIcon
                      size="md"
                      onClick={() =>
                        modals.openConfirmModal({
                          title: t('game.button.delete.all_traffic'),
                          children: <Text size="sm">{t('game.content.traffic.deleted_all_confirm')}</Text>,
                          onConfirm: onDeleteAll,
                          confirmProps: { color: 'red' },
                        })
                      }
                    >
                      <Icon path={mdiDeleteForeverOutline} size={1} />
                    </ActionIcon>
                  </Tooltip>
                  <Tooltip label={t('game.button.download.all_traffic')} position="left" classNames={tooltipClasses}>
                    <ActionIcon size="md" onClick={onDownloadAll}>
                      <Icon path={mdiDownloadMultiple} size={1} />
                    </ActionIcon>
                  </Tooltip>
                </Group>
              </Group>
              <Divider size="sm" />
              <ScrollSelect
                itemComponent={FileItem}
                itemComponentProps={{ onDownload, onDelete, disabled, t, locale }}
                items={orderedFileRecords}
                h={srollHeight}
              />
            </Grid.Col>
          </Grid>
        </Paper>
      )}
    </WithGameMonitor>
  )
}

export default Traffic
