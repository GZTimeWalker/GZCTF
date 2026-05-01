import {
  ActionIcon,
  Avatar,
  Badge,
  Center,
  Group,
  Modal,
  ModalProps,
  Progress,
  ScrollArea,
  Stack,
  Table,
  Text,
  Tooltip,
  useMantineTheme,
} from '@mantine/core'
import { mdiAccountArrowLeft, mdiAccountOutline, mdiClose, mdiTrophyVariantOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { MemberContributionPie } from '@Components/charts/MemberContributionPie'
import { MemberContributionPieProps } from '@Components/charts/MemberContributionPie'
import { TeamRadarMap, TeamRadarMapProps } from '@Components/charts/TeamRadarMap'
import { useLanguage } from '@Utils/I18n'
import { BloodsTypes, BonusLabel } from '@Utils/Shared'
import { ChallengeInfo, ScoreboardItem, ScoreboardModel, SubmissionType } from '@Api'
import modalClasses from '@Styles/ScoreboardItemModal.module.css'
import tableClasses from '@Styles/Table.module.css'
import { ScrollingText } from './ScrollingText'

export interface ScoreboardItemModalProps extends ModalProps {
  item?: ScoreboardItem | null
  divisionMap: Map<number, string>
  bloodBonusMap: Map<SubmissionType, BonusLabel>
  scoreboard?: ScoreboardModel
}

function calculateScoreRadar(
  challenges: Record<string, ChallengeInfo[]>,
  challengeIdMap: Map<number, ChallengeInfo>,
  item?: ScoreboardItem
): TeamRadarMapProps {
  const indicator =
    challenges &&
    Object.keys(challenges).map((cate) => ({
      name: cate,
      scoreSum: challenges[cate].reduce((sum, chal) => sum + (!chal.solved ? 0 : chal.score!), 0),
      max: 1,
    }))

  const value = indicator?.map((ind) => {
    const solvedChallenges = item?.solvedChallenges?.filter(
      (chal) => challengeIdMap?.get(chal.id!)?.category === ind.name
    )
    const cateScore = solvedChallenges?.reduce((sum, chal) => sum + chal.score!, 0) ?? 0
    return Math.min(cateScore / ind.scoreSum, 1)
  })

  return { indicator, value, name: item?.name ?? '' }
}

function calculateMemberContribution(item?: ScoreboardItem): MemberContributionPieProps {
  const memberScores =
    item?.solvedChallenges?.reduce((acc, chal) => {
      const score = acc.get(chal.userName!) ?? 0
      acc.set(chal.userName!, score + chal.score!)
      return acc
    }, new Map<string, number>()) ?? new Map<string, number>()

  const data = Array.from(memberScores.entries()).map(([name, value]) => ({ name, value }))
  data.sort((a, b) => b.value - a.value)
  return { data }
}

export const ScoreboardItemModal: FC<ScoreboardItemModalProps> = (props) => {
  const { item, scoreboard, bloodBonusMap, divisionMap, ...modalProps } = props
  const { t } = useTranslation()
  const { locale } = useLanguage()
  const theme = useMantineTheme()

  const challenges = scoreboard?.challenges
  const challengeIdMap =
    challenges &&
    Object.keys(challenges).reduce((map, key) => {
      challenges[key].forEach((challenge) => map.set(challenge.id!, challenge))
      return map
    }, new Map<number, ChallengeInfo>())

  const valid = item && challenges && challengeIdMap

  // ── Per-user drill-down ──────────────────────────────────────────────────
  const [selectedUser, setSelectedUser] = useState<string | null>(null)

  useEffect(() => { setSelectedUser(null) }, [item?.id])

  const userChallenges = useMemo(
    () => (item?.solvedChallenges ?? []).filter((c) => c.userName === selectedUser),
    [item?.solvedChallenges, selectedUser]
  )

  const userScore = useMemo(
    () => userChallenges.reduce((s, c) => s + (c.score ?? 0), 0),
    [userChallenges]
  )

  const userFirstBloods = useMemo(
    () => userChallenges.filter((c) => c.type && BloodsTypes.includes(c.type)).length,
    [userChallenges]
  )

  // Virtual item for radar when a user is selected
  const radarItem = useMemo(
    () => selectedUser ? { ...item!, solvedChallenges: userChallenges } : item,
    [selectedUser, item, userChallenges]
  )

  const teamSolveRatio = (item?.solvedCount ?? 0) / (scoreboard?.challengeCount ?? 1)
  const userSolveRatio = userChallenges.length / (scoreboard?.challengeCount ?? 1)

  const radarData = useMemo(() => {
    if (!valid) return null
    return calculateScoreRadar(challenges, challengeIdMap, radarItem ?? undefined)
  }, [valid, challenges, challengeIdMap, radarItem])

  const memberContributionData = useMemo(() => {
    if (!valid) return null
    return calculateMemberContribution(item)
  }, [valid, item])

  // All distinct members who contributed a solve
  const members = useMemo(
    () => [...new Set((item?.solvedChallenges ?? []).map((c) => c.userName).filter(Boolean))],
    [item?.solvedChallenges]
  )

  const visibleRows = selectedUser
    ? userChallenges.slice().sort((a, b) => dayjs(a.time).diff(dayjs(b.time)))
    : (item?.solvedChallenges ?? []).slice().sort((a, b) => dayjs(a.time).diff(dayjs(b.time)))

  return (
    <Modal
      {...modalProps}
      classNames={{ header: modalClasses.header, title: modalClasses.titleBar }}
      title={
        <Group justify="left" gap="md" wrap="nowrap" className={modalClasses.titleGroup}>
          <Avatar alt="avatar" src={item?.avatar} size={50} radius="md" className={modalClasses.avatar}>
            {item?.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Stack gap={0} className={modalClasses.infoWrap}>
            <Group gap={4} wrap="nowrap" className={modalClasses.nameRow}>
              <ScrollingText
                text={item?.name ?? 'Team'}
                size="lg"
                fw="bold"
                className={modalClasses.teamName}
                miw="5rem"
              />
              {item?.divisionId && (
                <Badge size="sm" variant="outline" className={modalClasses.divisionBadge}>
                  {divisionMap.get(item.divisionId) ?? 'Unknown'}
                </Badge>
              )}
            </Group>
            <ScrollingText
              text={item?.bio || t('team.placeholder.bio')}
              size="sm"
              className={modalClasses.bioText}
            />
          </Stack>
        </Group>
      }
    >
      <Stack align="center" gap="xs">
        <Stack w="85%" miw="20rem" gap="xs">

          {/* ── Charts ─────────────────────────────────────────────────── */}
          <Center h="14rem">
            {valid && radarData && (
              selectedUser ? (
                // User selected: show only radar for their category breakdown
                <TeamRadarMap {...radarData} />
              ) : (
                memberContributionData && (
                  <Group wrap="nowrap" gap={0} justify="center" w="100%" h="100%">
                    <TeamRadarMap {...radarData} />
                    <MemberContributionPie {...memberContributionData} />
                  </Group>
                )
              )
            )}
          </Center>

          {/* ── User drill-down banner ──────────────────────────────────── */}
          {selectedUser ? (
            <Group
              justify="space-between"
              px="sm"
              py={6}
              style={{
                borderRadius: theme.radius.sm,
                border: `1px solid ${theme.colors.blue[5]}`,
                backgroundColor: 'var(--mantine-color-blue-light)',
              }}
            >
              <Group gap="xs">
                <Icon path={mdiAccountOutline} size={0.85} color={theme.colors.blue[5]} />
                <Text size="sm" fw={700} c="blue">{selectedUser}</Text>
              </Group>
              <Tooltip label="Back to team view">
                <ActionIcon size="sm" variant="subtle" color="blue" onClick={() => setSelectedUser(null)}>
                  <Icon path={mdiClose} size={0.75} />
                </ActionIcon>
              </Tooltip>
            </Group>
          ) : (
            // Member chips — click to drill into a user
            members.length > 1 && (
              <Group gap={6} justify="center" wrap="wrap">
                {members.map((name) => (
                  <Badge
                    key={name}
                    variant="light"
                    color="gray"
                    size="sm"
                    leftSection={<Icon path={mdiAccountArrowLeft} size={0.55} />}
                    style={{ cursor: 'pointer' }}
                    onClick={() => setSelectedUser(name!)}
                  >
                    {name}
                  </Badge>
                ))}
              </Group>
            )
          )}

          {/* ── Stats bar ──────────────────────────────────────────────── */}
          <Group grow ta="center">
            {selectedUser ? (
              <>
                <Stack gap={2}>
                  <Text fw="bold" size="sm" ff="monospace">{userScore}</Text>
                  <Text size="xs" fw={500}>{t('game.label.score_table.score')}</Text>
                </Stack>
                <Stack gap={2}>
                  <Text fw="bold" size="sm" ff="monospace">{userChallenges.length}</Text>
                  <Text size="xs" fw={500}>{t('game.label.score_table.solved_count')}</Text>
                </Stack>
                {userFirstBloods > 0 && (
                  <Stack gap={2}>
                    <Text fw="bold" size="sm" ff="monospace" c="orange">{userFirstBloods}</Text>
                    <Text size="xs" fw={500}>Bloods</Text>
                  </Stack>
                )}
                <Stack gap={2}>
                  <Text fw="bold" size="sm" ff="monospace">
                    {item?.score ? `${Math.round(userScore / item.score * 100)}%` : '-'}
                  </Text>
                  <Text size="xs" fw={500}>Contribution</Text>
                </Stack>
              </>
            ) : (
              <>
                <Stack gap={2}>
                  <Text fw="bold" size="sm" ff="monospace">{item?.rank || '-'}</Text>
                  <Text size="xs" fw={500}>{t('game.label.score_table.rank_total')}</Text>
                </Stack>
                {item?.divisionId && (
                  <Stack gap={2}>
                    <Text fw="bold" size="sm" ff="monospace">{item?.divisionRank || '-'}</Text>
                    <Text size="xs" fw={500}>{t('game.label.score_table.rank_division')}</Text>
                  </Stack>
                )}
                <Stack gap={2}>
                  <Text fw="bold" size="sm" ff="monospace">{item?.score}</Text>
                  <Text size="xs" fw={500}>{t('game.label.score_table.score')}</Text>
                </Stack>
                <Stack gap={2}>
                  <Text fw="bold" size="sm" ff="monospace">{item?.solvedCount}</Text>
                  <Text size="xs" fw={500}>{t('game.label.score_table.solved_count')}</Text>
                </Stack>
              </>
            )}
          </Group>

          <Progress
            value={(selectedUser ? userSolveRatio : teamSolveRatio) * 100}
            color={selectedUser ? 'blue' : undefined}
          />
        </Stack>

        {/* ── Solve table ────────────────────────────────────────────────── */}
        {item?.solvedCount && item?.solvedCount > 0 ? (
          <ScrollArea scrollbarSize={6} h="12rem" w="100%" scrollbars="y">
            <Table className={tableClasses.table}>
              <Table.Thead>
                <Table.Tr>
                  {!selectedUser && (
                    <Table.Th>
                      <Text size="xs" c="dimmed" fs="italic">
                        {t('common.label.user')}
                      </Text>
                    </Table.Th>
                  )}
                  <Table.Th>{t('common.label.challenge')}</Table.Th>
                  <Table.Th>{t('game.label.score_table.type')}</Table.Th>
                  <Table.Th>{t('game.label.score_table.score')}</Table.Th>
                  <Table.Th>{t('common.label.time')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {challengeIdMap &&
                  visibleRows.map((chal, idx) => {
                    const info = challengeIdMap.get(chal.id!)
                    const isBlood = chal.type && BloodsTypes.includes(chal.type)
                    return (
                      <Table.Tr key={`${chal.id}-${idx}`}>
                        {!selectedUser && (
                          <Table.Td>
                            <Text
                              size="sm"
                              fw={600}
                              c="blue"
                              maw="8rem"
                              truncate
                              style={{ cursor: 'pointer' }}
                              onClick={() => setSelectedUser(chal.userName ?? null)}
                            >
                              {chal.userName ?? ''}
                            </Text>
                          </Table.Td>
                        )}
                        <Table.Td>
                          <ScrollingText text={info?.title ?? `#${chal.id}`} miw="10rem" maw="16rem" />
                        </Table.Td>
                        <Table.Td fz="sm">{info?.category}</Table.Td>
                        <Table.Td ff="monospace" fz="sm">
                          <Group gap={4} wrap="nowrap">
                            {isBlood && (
                              <Icon
                                path={mdiTrophyVariantOutline}
                                size={0.6}
                                color={theme.colors.orange[5]}
                              />
                            )}
                            {chal.score}
                            {info?.score && chal.score! > info.score && isBlood && (
                              <Text size="xs" c="dimmed" span>
                                {`+${bloodBonusMap.get(chal.type!)?.descr ?? ''}`}
                              </Text>
                            )}
                          </Group>
                        </Table.Td>
                        <Table.Td ff="monospace" fz="sm">
                          {dayjs(chal.time).locale(locale).format('SL HH:mm:ss')}
                        </Table.Td>
                      </Table.Tr>
                    )
                  })}
              </Table.Tbody>
            </Table>
          </ScrollArea>
        ) : (
          <Text py="1rem" fw="bold">
            {t('game.placeholder.no_solved')}
          </Text>
        )}
      </Stack>
    </Modal>
  )
}
