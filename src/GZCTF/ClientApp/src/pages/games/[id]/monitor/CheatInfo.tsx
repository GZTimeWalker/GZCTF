import {
  Accordion,
  Avatar,
  Badge,
  Box,
  Center,
  Group,
  Input,
  Paper,
  ScrollArea,
  Stack,
  Switch,
  Table,
  Text,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { useLocalStorage } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiKeyAlert, mdiTarget } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import WithGameMonitorTab from '@Components/WithGameMonitor'
import { RequireRole } from '@Components/WithRole'
import { ParticipationStatusControl } from '@Components/admin/ParticipationStatusControl'
import { SwitchLabel } from '@Components/admin/SwitchLabel'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useParticipationStatusMap } from '@Utils/Shared'
import { useDisplayInputStyles } from '@Utils/ThemeOverride'
import { OnceSWRConfig } from '@Utils/useConfig'
import { useUserRole } from '@Utils/useUser'
import api, { CheatInfoModel, ParticipationStatus, Role } from '@Api'
import classes from '@Styles/Accordion.module.css'

enum CheatType {
  Submit = 'Submit',
  Owned = 'Owned',
}

const CheatTypeMap = new Map([
  [
    CheatType.Submit,
    {
      color: 'orange',
      iconPath: mdiTarget,
    },
  ],
  [
    CheatType.Owned,
    {
      color: 'red',
      iconPath: mdiKeyAlert,
    },
  ],
])

interface CheatSubmissionInfo {
  time?: dayjs.Dayjs
  answer?: string
  user?: string
  challenge?: string
  relatedTeam?: string
  cheatType: CheatType
}

interface CheatTeamInfo {
  name?: string
  avatar?: string | null
  teamId?: number
  status?: ParticipationStatus
  lastSubmitTime?: dayjs.Dayjs
  participateId?: number
  organization?: string | null
  submissionInfo: Set<CheatSubmissionInfo>
}

const ToCheatTeamInfo = (cheatInfo: CheatInfoModel[]) => {
  const cheatTeamInfo = new Map<number, CheatTeamInfo>()
  for (const info of cheatInfo) {
    const { ownedTeam, submitTeam, submission } = info
    if (!ownedTeam || !submitTeam || !submission) continue

    const time = dayjs(submission.time)

    for (const part of [ownedTeam, submitTeam]) {
      if (!cheatTeamInfo.has(part.id ?? -1)) {
        cheatTeamInfo.set(part.id ?? -1, {
          name: part.team?.name,
          avatar: part.team?.avatar,
          teamId: part.team?.id,
          status: part.status,
          participateId: part.id,
          organization: part.organization,
          lastSubmitTime: time,
          submissionInfo: new Set<CheatSubmissionInfo>(),
        })
      }
    }

    const ownedTeamInfo = cheatTeamInfo.get(ownedTeam.id ?? -1)
    const submitTeamInfo = cheatTeamInfo.get(submitTeam.id ?? -1)

    if (!ownedTeamInfo || !submitTeamInfo) continue

    if (ownedTeamInfo.lastSubmitTime?.isBefore(time)) {
      ownedTeamInfo.lastSubmitTime = time
    }

    const cheatSubmissionInfo: CheatSubmissionInfo = {
      time: time,
      answer: submission.answer,
      user: submission.user,
      challenge: submission.challenge,
      cheatType: CheatType.Owned,
      relatedTeam: submitTeam.team?.name,
    }

    ownedTeamInfo.submissionInfo.add(cheatSubmissionInfo)

    if (submitTeamInfo.lastSubmitTime?.isBefore(time)) {
      submitTeamInfo.lastSubmitTime = time
    }

    const cheatSubmissionSourceInfo: CheatSubmissionInfo = {
      ...cheatSubmissionInfo,
      cheatType: CheatType.Submit,
      relatedTeam: ownedTeam.team?.name,
    }

    submitTeamInfo.submissionInfo.add(cheatSubmissionSourceInfo)
  }
  return cheatTeamInfo
}

interface CheatSubmissionInfoProps {
  submissionInfo: CheatSubmissionInfo
}

const CheatSubmissionInfo: FC<CheatSubmissionInfoProps> = (props) => {
  const { submissionInfo } = props
  const theme = useMantineTheme()
  const type = CheatTypeMap.get(submissionInfo.cheatType)!
  const { classes } = useDisplayInputStyles({ ff: 'monospace' })

  return (
    <Group justify="space-between" w="100%" gap={0}>
      <Group justify="space-between" w="60%" pr="2rem">
        <Group justify="left">
          <Icon path={type.iconPath} size={1} color={theme.colors[type.color][6]} />
          <Badge size="sm" color="indigo">
            {dayjs(submissionInfo.time).format('MM/DD HH:mm:ss')}
          </Badge>
          <Text lineClamp={1} fw="bold">
            {submissionInfo.relatedTeam}
          </Text>
        </Group>
        <Text size="sm" lineClamp={1} fw="bold">
          {submissionInfo.user}
        </Text>
      </Group>
      <Stack gap={0} w="40%">
        <Text fw="bold" size="xs" lineClamp={1}>
          {submissionInfo.challenge}
        </Text>
        <Input
          variant="unstyled"
          value={submissionInfo.answer}
          readOnly
          size="xs"
          classNames={classes}
        />
      </Stack>
    </Group>
  )
}

interface CheatInfoItemProps {
  userRole: Role
  disabled: boolean
  cheatTeamInfo: CheatTeamInfo
  setParticipationStatus: (id: number, status: ParticipationStatus) => Promise<void>
}

const CheatInfoItem: FC<CheatInfoItemProps> = (props) => {
  const { cheatTeamInfo, disabled, userRole, setParticipationStatus } = props
  const theme = useMantineTheme()
  const part = useParticipationStatusMap().get(cheatTeamInfo.status!)!

  const { t } = useTranslation()

  return (
    <Accordion.Item value={cheatTeamInfo.participateId!.toString()}>
      <Box display="flex" style={{ alignItems: 'center' }}>
        <Accordion.Control>
          <Group justify="space-between">
            <Group justify="left">
              <Avatar alt="avatar" src={cheatTeamInfo.avatar}>
                {!cheatTeamInfo.name ? 'T' : cheatTeamInfo.name.slice(0, 1)}
              </Avatar>
              <Stack gap={0}>
                <Group gap={4}>
                  <Title order={4} lineClamp={1} fw="bold">
                    {!cheatTeamInfo.name
                      ? t('admin.placeholder.games.participation.team')
                      : cheatTeamInfo.name}
                  </Title>
                  {cheatTeamInfo?.organization && (
                    <Badge size="sm" variant="outline">
                      {cheatTeamInfo.organization}
                    </Badge>
                  )}
                </Group>
                <Text size="sm" lineClamp={1}>
                  {dayjs(cheatTeamInfo.lastSubmitTime).format('MM/DD HH:mm:ss')}
                </Text>
              </Stack>
            </Group>
            <Group w="12rem" gap={0} justify="space-between" wrap="nowrap">
              <Box w="6rem" ta="center">
                <Badge color={part.color}>{part.title}</Badge>
              </Box>
              {RequireRole(Role.Admin, userRole) && (
                <ParticipationStatusControl
                  disabled={disabled}
                  participateId={cheatTeamInfo.participateId!}
                  status={cheatTeamInfo.status!}
                  setParticipationStatus={setParticipationStatus}
                  m={`0 ${theme.spacing.xl}`}
                  miw={theme.spacing.xl}
                />
              )}
            </Group>
          </Group>
        </Accordion.Control>
      </Box>
      <Accordion.Panel>
        <Stack gap="sm">
          {[...cheatTeamInfo.submissionInfo]
            .sort((a, b) => (b.time?.unix() ?? 0) - (a.time?.unix() ?? 0))
            .map((submissionInfo) => (
              <CheatSubmissionInfo
                key={submissionInfo.time?.unix()}
                submissionInfo={submissionInfo}
              />
            ))}
        </Stack>
      </Accordion.Panel>
    </Accordion.Item>
  )
}

interface CheatInfoTeamViewProps {
  disabled: boolean
  cheatTeamInfo: Map<number, CheatTeamInfo>
  setParticipationStatus: (id: number, status: ParticipationStatus) => Promise<void>
}

const CheatInfoTeamView: FC<CheatInfoTeamViewProps> = (props) => {
  const { role } = useUserRole()
  const { cheatTeamInfo, disabled, setParticipationStatus } = props

  const { t } = useTranslation()

  return (
    <ScrollArea offsetScrollbars h="calc(100vh - 180px)">
      <Stack gap="xs" w="100%">
        {!cheatTeamInfo || cheatTeamInfo?.size === 0 ? (
          <Center h="calc(100vh - 200px)">
            <Stack gap={0}>
              <Title order={2}>{t('game.content.no_cheat.title')}</Title>
              <Text>{t('game.content.no_cheat.comment')}</Text>
            </Stack>
          </Center>
        ) : (
          <Accordion
            multiple
            variant="contained"
            chevronPosition="left"
            classNames={classes}
            className={classes.root}
          >
            {[...cheatTeamInfo.values()]
              .sort((a, b) => (b.lastSubmitTime?.unix() ?? 0) - (a.lastSubmitTime?.unix() ?? 0))
              .map((cheatInfo) => (
                <CheatInfoItem
                  key={cheatInfo.participateId}
                  userRole={role ?? Role.User}
                  cheatTeamInfo={cheatInfo}
                  disabled={disabled}
                  setParticipationStatus={setParticipationStatus}
                />
              ))}
          </Accordion>
        )}
      </Stack>
    </ScrollArea>
  )
}

interface CheatInfoTableViewProps {
  cheatInfo: CheatInfoModel[]
}

const CheatInfoTableView: FC<CheatInfoTableViewProps> = (props) => {
  const { classes: inputClasses } = useDisplayInputStyles({ ff: 'monospace' })
  const { t } = useTranslation()

  const rows = props.cheatInfo
    .sort(
      (a, b) => (dayjs(b.submission?.time).unix() ?? 0) - (dayjs(a.submission?.time).unix() ?? 0)
    )
    .map((item, i) => (
      <Table.Tr key={`${item.submission?.time}@${i}`}>
        <Table.Td ff="monospace">
          <Badge size="sm" color="indigo">
            {dayjs(item.submission?.time).format('MM/DD HH:mm:ss')}
          </Badge>
        </Table.Td>
        <Table.Td>
          <Text size="sm" fw="bold">
            {item.ownedTeam?.team?.name ?? 'Team'}
          </Text>
        </Table.Td>
        <Table.Td>
          <Badge size="sm" color="orange">
            {`>>>`}
          </Badge>
        </Table.Td>
        <Table.Td>
          <Text size="sm" fw="bold">
            {item.submitTeam?.team?.name ?? 'Team'}
          </Text>
        </Table.Td>
        <Table.Td>
          <Text ff="monospace" size="sm" fw="bold">
            {item.submission?.user ?? 'User'}
          </Text>
        </Table.Td>
        <Table.Td>{item.submission?.challenge ?? 'Challenge'}</Table.Td>
        <Table.Td
          style={{
            width: '24vw',
            padding: 0,
          }}
        >
          <Input
            variant="unstyled"
            value={item.submission?.answer}
            readOnly
            size="sm"
            classNames={inputClasses}
          />
        </Table.Td>
      </Table.Tr>
    ))

  return (
    <Paper shadow="md" p="md">
      <ScrollArea offsetScrollbars h="calc(100vh - 200px)">
        <Table className={classes.table}>
          <Table.Thead>
            <Table.Tr>
              <Table.Th style={{ width: '8rem' }}>{t('common.label.time')}</Table.Th>
              <Table.Th style={{ minWidth: '5rem' }}>
                {t('game.label.cheat_info.owned_team')}
              </Table.Th>
              <Table.Th />
              <Table.Th style={{ minWidth: '5rem' }}>
                {t('game.label.cheat_info.submit_team')}
              </Table.Th>
              <Table.Th style={{ minWidth: '5rem' }}>
                {t('game.label.cheat_info.submit_user')}
              </Table.Th>
              <Table.Th style={{ minWidth: '3rem' }}>{t('common.label.challenge')}</Table.Th>
              <Table.Th className={classes.mono}>{t('common.label.flag')}</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>{rows}</Table.Tbody>
        </Table>
      </ScrollArea>
    </Paper>
  )
}

const CheatInfo: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { data: cheatInfo } = api.game.useGameCheatInfo(numId, OnceSWRConfig)

  const [disabled, setDisabled] = useState(false)
  const [cheatTeamInfo, setCheatTeamInfo] = useState<Map<number, CheatTeamInfo>>()
  const [teamView, setTeamView] = useLocalStorage({
    key: 'cheat-info-team-view',
    defaultValue: true,
    getInitialValueInEffect: false,
  })

  const { t } = useTranslation()

  useEffect(() => {
    if (!cheatInfo) return

    setCheatTeamInfo(ToCheatTeamInfo(cheatInfo))
  }, [cheatInfo])

  const setParticipationStatus = async (id: number, status: ParticipationStatus) => {
    setDisabled(true)
    try {
      await api.admin.adminParticipation(id, status)
      cheatTeamInfo &&
        setCheatTeamInfo(
          cheatTeamInfo.set(id, {
            ...cheatTeamInfo.get(id)!,
            status,
          })
        )
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.participation.updated'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (err: any) {
      showErrorNotification(err, t)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <WithGameMonitorTab isLoading={!cheatInfo}>
      <Group justify="space-between" w="100%">
        <Switch
          label={SwitchLabel(
            t('game.content.team_view.label'),
            t('game.content.team_view.description')
          )}
          checked={teamView}
          onChange={(e) => setTeamView(e.currentTarget.checked)}
        />
      </Group>
      {teamView ? (
        <CheatInfoTeamView
          disabled={disabled}
          cheatTeamInfo={cheatTeamInfo ?? new Map()}
          setParticipationStatus={setParticipationStatus}
        />
      ) : (
        <CheatInfoTableView cheatInfo={cheatInfo ?? []} />
      )}
    </WithGameMonitorTab>
  )
}

export default CheatInfo
