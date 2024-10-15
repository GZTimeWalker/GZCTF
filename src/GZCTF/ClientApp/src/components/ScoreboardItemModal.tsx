import {
  Avatar,
  Badge,
  Center,
  Group,
  LoadingOverlay,
  Modal,
  ModalProps,
  Progress,
  ScrollArea,
  Stack,
  Table,
  Text,
  Title,
  Input,
} from '@mantine/core'
import dayjs from 'dayjs'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import TeamRadarMap from '@Components/TeamRadarMap'
import { BloodsTypes, BonusLabel } from '@Utils/Shared'
import { ChallengeInfo, ScoreboardItem, ScoreboardModel, SubmissionType } from '@Api'
import inputClasses from '@Styles/Input.module.css'
import tableClasses from '@Styles/Table.module.css'

export interface ScoreboardItemModalProps extends ModalProps {
  item?: ScoreboardItem | null
  bloodBonusMap: Map<SubmissionType, BonusLabel>
  scoreboard?: ScoreboardModel
}

const ScoreboardItemModal: FC<ScoreboardItemModalProps> = (props) => {
  const { item, scoreboard, bloodBonusMap, ...modalProps } = props

  const { t } = useTranslation()

  const challenges = scoreboard?.challenges
  const challengeIdMap =
    challenges &&
    Object.keys(challenges).reduce((map, key) => {
      challenges[key].forEach((challenge) => {
        map.set(challenge.id!, challenge)
      })
      return map
    }, new Map<number, ChallengeInfo>())

  const solved = (item?.solvedCount ?? 0) / (scoreboard?.challengeCount ?? 1)

  const indicator =
    challenges &&
    Object.keys(challenges).map((cate) => ({
      name: cate,
      scoreSum: challenges[cate].reduce((sum, chal) => sum + (!chal.solved ? 0 : chal.score!), 0),
      max: 1,
    }))

  const values = indicator?.map((ind) => {
    const solvedChallenges = item?.solvedChallenges?.filter(
      (chal) => challengeIdMap?.get(chal.id!)?.category === ind.name
    )
    const cateScore = solvedChallenges?.reduce((sum, chal) => sum + chal.score!, 0) ?? 0
    return Math.min(cateScore / ind.scoreSum, 1)
  })

  return (
    <Modal
      {...modalProps}
      title={
        <Group justify="left" gap="md" wrap="nowrap">
          <Avatar alt="avatar" src={item?.avatar} size={50} radius="md">
            {item?.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Stack gap={0}>
            <Group gap={4}>
              <Title order={4} lineClamp={1} fw="bold">
                {item?.name ?? 'Team'}
              </Title>
              {item?.organization && (
                <Badge size="sm" variant="outline">
                  {item.organization}
                </Badge>
              )}
            </Group>
            <Text truncate size="sm" lineClamp={1}>
              {item?.bio || t('team.placeholder.bio')}
            </Text>
          </Stack>
        </Group>
      }
    >
      <Stack align="center" gap="xs">
        <Stack w="60%" miw="20rem">
          <Center h="14rem">
            <LoadingOverlay visible={!indicator || !values} />
            {item && indicator && values && (
              <TeamRadarMap indicator={indicator} value={values} name={item?.name ?? ''} />
            )}
          </Center>
          <Group grow ta="center">
            <Stack gap={2}>
              <Text fw="bold" size="sm" ff="monospace">
                {item?.rank}
              </Text>
              <Text size="xs" fw={500}>
                {t('game.label.score_table.rank_total')}
              </Text>
            </Stack>
            {item?.organization && (
              <Stack gap={2}>
                <Text fw="bold" size="sm" ff="monospace">
                  {item?.organizationRank}
                </Text>
                <Text size="xs" fw={500}>
                  {t('game.label.score_table.rank_organization')}
                </Text>
              </Stack>
            )}
            <Stack gap={2}>
              <Text fw="bold" size="sm" ff="monospace">
                {item?.score}
              </Text>
              <Text size="xs" fw={500}>
                {t('game.label.score_table.score')}
              </Text>
            </Stack>
            <Stack gap={2}>
              <Text fw="bold" size="sm" ff="monospace">
                {item?.solvedCount}
              </Text>
              <Text size="xs" fw={500}>
                {t('game.label.score_table.solved_count')}
              </Text>
            </Stack>
          </Group>
          <Progress value={solved * 100} />
        </Stack>
        {item?.solvedCount && item?.solvedCount > 0 ? (
          <ScrollArea scrollbarSize={6} h="12rem" w="100%" scrollbars="y">
            <Table className={tableClasses.table}>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('common.label.user')}</Table.Th>
                  <Table.Th>{t('common.label.challenge')}</Table.Th>
                  <Table.Th>{t('game.label.score_table.type')}</Table.Th>
                  <Table.Th>{t('game.label.score_table.score')}</Table.Th>
                  <Table.Th>{t('common.label.time')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {item?.solvedChallenges &&
                  challengeIdMap &&
                  item.solvedChallenges
                    .sort((a, b) => dayjs(b.time).diff(dayjs(a.time)))
                    .map((chal) => {
                      const info = challengeIdMap.get(chal.id!)!
                      return (
                        <Table.Tr key={chal.id}>
                          <Table.Td fw="bold">{chal.userName}</Table.Td>
                          <Table.Td>
                            <Input
                              variant="unstyled"
                              value={info.title}
                              readOnly
                              size="sm"
                              miw="16rem"
                              maw="20rem"
                              __vars={{
                                '--input-height': 'var(--mantine-line-height-sm)',
                              }}
                              classNames={{
                                input: inputClasses.input,
                                wrapper: inputClasses.wrapper,
                              }}
                            />
                          </Table.Td>
                          <Table.Td ff="monospace" fz="sm">
                            {info.category}
                          </Table.Td>
                          <Table.Td ff="monospace" fz="sm">
                            {chal.score}
                            {info.score &&
                              chal.score! > info.score &&
                              chal.type &&
                              BloodsTypes.includes(chal.type) && (
                                <Text size="sm" c="dimmed" span>
                                  {`(${bloodBonusMap.get(chal.type)?.descr})`}
                                </Text>
                              )}
                          </Table.Td>
                          <Table.Td ff="monospace" fz="sm">
                            {dayjs(chal.time).format('MM/DD HH:mm:ss')}
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

export default ScoreboardItemModal
