import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  Center,
  ScrollArea,
  Stack,
  Text,
  Title,
  Group,
  Accordion,
  createStyles,
  useMantineTheme,
  Box,
  Avatar,
  Badge,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import WithGameMonitorTab from '@Components/WithGameMonitor'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { ParticipationStatusMap } from '@Utils/Shared'
import api, { CheatInfoModel, ParticipationStatus } from '@Api'

enum CheatType {
  Submit = 'Submit',
  Owned = 'Owned',
}

interface CheatSubmissionInfo {
  time?: dayjs.Dayjs
  answer?: string
  user?: string
  challenge?: string
  cheatType: CheatType
}

interface CheatTeamInfo {
  name?: string
  avatar?: string | null
  lastSubmitTime?: dayjs.Dayjs
  teamId?: number
  status?: ParticipationStatus
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
    }

    ownedTeamInfo.submissionInfo.add(cheatSubmissionInfo)

    if (submitTeamInfo.lastSubmitTime?.isBefore(time)) {
      submitTeamInfo.lastSubmitTime = time
    }

    const cheatSubmissionSourceInfo: CheatSubmissionInfo = {
      ...cheatSubmissionInfo,
      cheatType: CheatType.Submit,
    }

    submitTeamInfo.submissionInfo.add(cheatSubmissionSourceInfo)
  }
  return cheatTeamInfo
}

const useStyles = createStyles((theme) => ({
  root: {
    backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[6] : theme.colors.gray[0],
    borderRadius: theme.radius.sm,
  },

  item: {
    backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[6] : theme.colors.gray[0],
    border: '1px solid rgba(0,0,0,0.2)',
    position: 'relative',
    transition: 'transform 150ms ease',

    '&[data-active]': {
      backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[7] : theme.white,
      boxShadow: theme.shadows.md,
    },
  },

  label: {
    padding: '0',
  },

  control: {
    padding: '8px 4px',
    ...theme.fn.hover({ background: 'transparent' }),
  },
}))

interface CheatSubmissionInfoProps {
  submissionInfo: CheatSubmissionInfo
}

const CheatSubmissionInfo: FC<CheatSubmissionInfoProps> = (props) => {
  const { submissionInfo } = props

  return (
    <Group position="apart" w="100%">
      <Group position="apart" w="50%">
        <Text>{submissionInfo.time?.format('YYYY-MM-DD HH:mm:ss')}</Text>
        <Text>{submissionInfo.user}</Text>

        <Text>{submissionInfo.challenge}</Text>
      </Group>

      <Group position="apart" w="50%">
        <Text>{submissionInfo.answer}</Text>
        <Text>{submissionInfo.cheatType}</Text>
      </Group>
    </Group>
  )
}

interface CheatInfoItemProps {
  cheatTeamInfo: CheatTeamInfo
  disabled: boolean
  setParticipationStatus: (id: number, status: ParticipationStatus) => Promise<void>
}

const CheatInfoItem: FC<CheatInfoItemProps> = (props) => {
  const { cheatTeamInfo, disabled, setParticipationStatus } = props
  const theme = useMantineTheme()

  return (
    <Accordion.Item value={cheatTeamInfo.participateId!.toString()}>
      <Box sx={{ display: 'flex', alignItems: 'center' }}>
        <Accordion.Control>
          <Group position="apart">
            <Group position="left">
              <Avatar alt="avatar" src={cheatTeamInfo.avatar}>
                {!cheatTeamInfo.name ? 'T' : cheatTeamInfo.name.slice(0, 1)}
              </Avatar>
              <Stack spacing={0}>
                <Group spacing={4}>
                  <Title order={4} lineClamp={1} weight="bold">
                    {!cheatTeamInfo.name ? '（无名队伍）' : cheatTeamInfo.name}
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
            <Box w="5em">
              <Badge color={ParticipationStatusMap.get(cheatTeamInfo.status!)?.color}>
                {ParticipationStatusMap.get(cheatTeamInfo.status!)?.title}
              </Badge>
            </Box>
          </Group>
        </Accordion.Control>
        <Group m={`0 ${theme.spacing.xl}`} miw={theme.spacing.xl} position="right">
          {ParticipationStatusMap.get(cheatTeamInfo.status!)?.transformTo.map((value) => {
            const s = ParticipationStatusMap.get(value)!
            return (
              <ActionIconWithConfirm
                key={`${cheatTeamInfo.participateId}@${value}`}
                iconPath={s.iconPath}
                color={s.color}
                message={`确定要设为“${s.title}”吗？`}
                disabled={disabled}
                onClick={() => setParticipationStatus(cheatTeamInfo.participateId!, value)}
              />
            )
          })}
        </Group>
      </Box>
      <Accordion.Panel>
        <Stack>
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

const CheatInfo: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { data: cheatInfo } = api.game.useGameCheatInfo(numId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })

  const { classes } = useStyles()
  const [cheatTeamInfo, setCheatTeamInfo] = useState<Map<number, CheatTeamInfo>>()
  const [disabled, setDisabled] = useState(false)

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
        title: '操作成功',
        message: '参与状态已更新',
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (err: any) {
      showErrorNotification(err)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <WithGameMonitorTab>
      <ScrollArea offsetScrollbars h="calc(100vh - 160px)">
        <Stack spacing="xs" pr={10} w="100%">
          {!cheatTeamInfo || cheatTeamInfo?.size === 0 ? (
            <Center h="calc(100vh - 180px)">
              <Stack spacing={0}>
                <Title order={2}>暂时没有队伍作弊信息</Title>
                <Text>看起来大家都很老实呢</Text>
              </Stack>
            </Center>
          ) : (
            <Accordion
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
                    cheatTeamInfo={cheatInfo}
                    disabled={disabled}
                    setParticipationStatus={setParticipationStatus}
                  />
                ))}
            </Accordion>
          )}
        </Stack>
      </ScrollArea>
    </WithGameMonitorTab>
  )
}

export default CheatInfo
