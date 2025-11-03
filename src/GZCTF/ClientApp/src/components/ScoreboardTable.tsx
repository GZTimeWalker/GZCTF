import {
  alpha,
  Avatar,
  Box,
  Center,
  Grid,
  Group,
  Pagination,
  Paper,
  Select,
  Stack,
  Table,
  Text,
  TextInput,
  Tooltip,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { mdiAccountGroup, mdiMagnify, mdiFlagOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import cx from 'clsx'
import dayjs from 'dayjs'
import React, { FC, useEffect, useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { ScoreboardItemModal } from '@Components/ScoreboardItemModal'
import { ScrollingText } from '@Components/ScrollingText'
import { useLanguage } from '@Utils/I18n'
import {
  BloodBonus,
  BloodsTypes,
  useChallengeCategoryLabelMap,
  SubmissionTypeIconMap,
  useBonusLabels,
  PartialIconProps,
} from '@Utils/Shared'
import { useGameScoreboard } from '@Hooks/useGame'
import { ChallengeInfo, ChallengeCategory, ScoreboardItem, SubmissionType } from '@Api'
import misc from '@Styles/Misc.module.css'
import classes from '@Styles/ScoreboardTable.module.css'

const Widths = [60, 60, 175, 60, 70, 60]
const Lefts = Widths.reduce(
  (acc, cur) => {
    acc.push(acc[acc.length - 1] + cur)
    return acc
  },
  [0]
)

const TableHeader = React.memo((table: Record<string, ChallengeInfo[]>) => {
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()
  const { t } = useTranslation()
  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()

  const hiddenCol = [...Array(5).keys()].map((i) => (
    <Table.Th
      key={i}
      className={classes.left}
      style={{
        left: Lefts[i],
        width: Widths[i],
        minWidth: Widths[i],
        maxWidth: Widths[i],
      }}
    >
      &nbsp;
    </Table.Th>
  ))

  return (
    <Table.Thead className={classes.thead}>
      <Table.Tr className={misc.noBorder}>
        {hiddenCol}
        {Object.keys(table).map((key) => {
          const cate = challengeCategoryLabelMap.get(key as ChallengeCategory)!
          return (
            <Table.Th
              key={key}
              colSpan={table[key].length}
              h="3rem"
              style={{
                backgroundColor: alpha(
                  theme.colors[cate.color][colorScheme === 'dark' ? 8 : 6],
                  colorScheme === 'dark' ? 0.15 : 0.2
                ),
              }}
            >
              <Group gap={4} wrap="nowrap" justify="center" w="100%">
                <Icon path={cate.icon} size={1} color={theme.colors[cate.color][colorScheme === 'dark' ? 8 : 6]} />
                <Text c={cate.color} className={classes.text} ff="text" fz="sm">
                  {key}
                </Text>
              </Group>
            </Table.Th>
          )
        })}
      </Table.Tr>
      {/* Challenge Name */}
      <Table.Tr>
        {hiddenCol}
        {Object.keys(table).map((key) => table[key].map((item) => <Table.Th key={item.id}>{item.title}</Table.Th>))}
      </Table.Tr>
      {/* Headers & Score */}
      <Table.Tr>
        {[
          t('game.label.score_table.rank_total'),
          t('game.label.score_table.rank_division'),
          t('common.label.team'),
          t('game.label.score_table.solved_count'),
          t('game.label.score_table.score_total'),
        ].map((header, idx) => (
          <Table.Th key={idx} className={cx(classes.left, classes.header)} style={{ left: Lefts[idx] }}>
            {header}
          </Table.Th>
        ))}
        {Object.keys(table).map((key) =>
          table[key].map((item) => (
            <Table.Th key={item.id} className={classes.mono}>
              {item.score}
            </Table.Th>
          ))
        )}
      </Table.Tr>
    </Table.Thead>
  )
})

const TableRow: FC<{
  item: ScoreboardItem
  allRank: boolean
  tableRank: number
  onOpenDetail: () => void
  iconMap: Map<SubmissionType, PartialIconProps | undefined>
  challenges?: Record<string, ChallengeInfo[]>
  divisionMap: Map<number, string>
}> = React.memo(({ item, challenges, onOpenDetail, iconMap, tableRank, allRank, divisionMap }) => {
  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()
  const solved = item.solvedChallenges
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()
  const { locale } = useLanguage()
  const divisionName =
    item.divisionId !== undefined && item.divisionId !== null ? divisionMap.get(item.divisionId) : undefined

  const zeroScoreIcon = useMemo(() => {
    const normalIcon = iconMap.get(SubmissionType.Normal)
    const color = colorScheme === 'dark' ? theme.colors.gray[4] : theme.colors.gray[6]

    return {
      path: mdiFlagOutline,
      size: normalIcon?.size ?? 1,
      color,
    }
  }, [iconMap, theme, colorScheme])

  const totalScore = useMemo(() => {
    return solved?.reduce((acc, cur) => acc + (cur?.score ?? 0), 0) ?? 0
  }, [solved])

  return (
    <Table.Tr>
      <Table.Td className={cx(classes.mono, classes.left)} style={{ left: Lefts[0] }}>
        {item.rank || '-'}
      </Table.Td>
      <Table.Td className={cx(classes.mono, classes.left)} style={{ left: Lefts[1] }}>
        {allRank ? item.rank : (item.divisionRank ?? tableRank)}
      </Table.Td>
      <Table.Td className={classes.left} style={{ left: Lefts[2] }}>
        <Group
          justify="left"
          gap={5}
          wrap="nowrap"
          onClick={onOpenDetail}
          maw={Widths[2] - 10}
          className={classes.pointer}
        >
          <Avatar alt="avatar" src={item.avatar} radius="xl" size={30} color={theme.primaryColor}>
            {item.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Stack gap={0} h="2.5rem" justify="center" w={Widths[2] - 45}>
            <ScrollingText size="sm" text={item.name || ''} onClick={onOpenDetail} />
            {!!divisionName && (
              <Text size="xs" c="dimmed" ta="start" truncate className={classes.text}>
                {divisionName}
              </Text>
            )}
          </Stack>
        </Group>
      </Table.Td>
      <Table.Td className={cx(classes.mono, classes.left)} style={{ left: Lefts[3] }}>
        {solved?.length}
      </Table.Td>
      <Table.Td className={cx(classes.mono, classes.left)} style={{ left: Lefts[4] }}>
        {totalScore}
      </Table.Td>
      {challenges &&
        Object.keys(challenges).map((key) =>
          challenges[key].map((item) => {
            const chal = solved?.find((c) => c.id === item.id)
            const isZeroScore = chal && chal.type === SubmissionType.Normal && (chal.score ?? 0) === 0
            const icon = isZeroScore ? zeroScoreIcon : iconMap.get(chal?.type ?? SubmissionType.Unaccepted)

            if (!icon) return <Table.Td key={item.id} className={classes.mono} />

            const cate = challengeCategoryLabelMap.get(item.category as ChallengeCategory)!

            return (
              <Table.Td key={item.id} className={classes.mono}>
                <Tooltip
                  transitionProps={{ transition: 'pop' }}
                  label={
                    <Stack align="flex-start" gap={0} maw="20rem">
                      <Text lineClamp={3} fz="xs" className={classes.text}>
                        {item.title}
                      </Text>
                      <Text c={cate.color} fz="xs" className={cx(classes.text, classes.mono)}>
                        + {chal?.score} pts
                      </Text>
                      <Text c="dimmed" fz="xs" className={cx(classes.text, classes.mono)}>
                        # {dayjs(chal?.time).locale(locale).format('L LTS')}
                      </Text>
                    </Stack>
                  }
                >
                  <Center>
                    <Icon {...icon} />
                  </Center>
                </Tooltip>
              </Table.Td>
            )
          })
        )}
    </Table.Tr>
  )
})

const ITEM_COUNT_PER_PAGE = 30

export interface ScoreboardProps {
  divisionId: number | null
  setDivisionId: (div: number | null) => void
}

export const ScoreboardTable: FC<ScoreboardProps> = ({ divisionId, setDivisionId }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { iconMap } = SubmissionTypeIconMap(1)
  const [activePage, setPage] = useState(1)
  const [bloodBonus, setBloodBonus] = useState(BloodBonus.default)

  const [keyword, setKeyword] = useState('')
  const [debouncedKeyword] = useDebouncedValue(keyword, 400)

  const { scoreboard } = useGameScoreboard(numId)

  const divisionMap = useMemo(() => {
    const map = new Map<number, string>()
    scoreboard?.divisions?.forEach((div) => {
      map.set(div.id, div.name.trim())
    })
    return map
  }, [scoreboard?.divisions])

  const divisionOptions = useMemo(
    () =>
      (scoreboard?.divisions ?? []).map((div) => ({
        value: div.id.toString(),
        label: div.name.trim(),
      })),
    [scoreboard?.divisions]
  )

  const selectValue = useMemo(() => (divisionId === null ? 'all' : divisionId.toString()), [divisionId])

  useEffect(() => {
    if (divisionId !== null && !divisionMap.has(divisionId)) {
      setDivisionId(null)
    }
  }, [divisionMap, divisionId, setDivisionId])

  const filteredList = useMemo(() => {
    if (!scoreboard?.items) return []

    if (!!debouncedKeyword && debouncedKeyword.length > 0) {
      return scoreboard.items.filter((s) => s.name?.toLowerCase().includes(debouncedKeyword.toLowerCase()))
    }

    if (divisionId !== null) {
      return scoreboard.items.filter((s) => (s.divisionId ?? null) === divisionId)
    }

    return scoreboard.items.filter((s) => s.rank > 0)
  }, [scoreboard, debouncedKeyword, divisionId])

  useEffect(() => {
    setPage(1)
    setDivisionId(null)
    setKeyword('')
  }, [id, setDivisionId])

  const base = (activePage - 1) * ITEM_COUNT_PER_PAGE
  const currentItems = filteredList?.slice(base, base + ITEM_COUNT_PER_PAGE)

  const [currentItem, setCurrentItem] = useState<ScoreboardItem | null>(null)
  const [itemDetailOpened, setItemDetailOpened] = useState(false)

  const { t } = useTranslation()

  useEffect(() => {
    if (scoreboard) {
      setBloodBonus(new BloodBonus(scoreboard.bloodBonus))
    }
  }, [scoreboard])

  const bloodData = useBonusLabels(bloodBonus)
  const hasDivisionFilter = divisionOptions.length > 0

  return (
    <Paper shadow="md" p="md">
      <Stack gap="xs">
        <Grid>
          <Grid.Col span={3}>
            <Select
              defaultValue="all"
              data={[{ value: 'all', label: t('game.label.score_table.all_teams') }, ...divisionOptions]}
              value={selectValue}
              readOnly={!hasDivisionFilter}
              onChange={(div) => {
                if (!div || div === 'all') {
                  setDivisionId(null)
                } else {
                  const parsed = Number(div)
                  setDivisionId(Number.isNaN(parsed) ? null : parsed)
                }
                setPage(1)
              }}
              leftSection={<Icon path={mdiAccountGroup} size={1} />}
            />
          </Grid.Col>
          <Grid.Col span={6} />
          <Grid.Col span={3}>
            <TextInput
              placeholder={t('game.placeholder.search_team')}
              value={keyword}
              onChange={(e) => setKeyword(e.currentTarget.value)}
              leftSection={<Icon path={mdiMagnify} size={1} />}
            />
          </Grid.Col>
        </Grid>
        <Box pos="relative" mih="calc(100vh - 14rem)">
          <Table.ScrollContainer
            minWidth="100%"
            classNames={{
              scrollContainer: misc.noScrollBars,
            }}
          >
            <Table className={classes.table}>
              <TableHeader {...scoreboard?.challenges} />
              <Table.Tbody>
                {scoreboard &&
                  currentItems?.map((item, idx) => (
                    <TableRow
                      key={base + idx}
                      allRank={divisionId === null}
                      tableRank={base + idx + 1}
                      item={item}
                      onOpenDetail={() => {
                        setCurrentItem(item)
                        setItemDetailOpened(true)
                      }}
                      challenges={scoreboard.challenges}
                      iconMap={iconMap}
                      divisionMap={divisionMap}
                    />
                  ))}
              </Table.Tbody>
            </Table>
          </Table.ScrollContainer>
          <Box className={classes.legend}>
            <Stack gap="xs">
              <Tooltip.Group>
                <Group gap="lg">
                  {BloodsTypes.map((type, idx) => (
                    <Tooltip key={idx} label={bloodData.get(type)?.name} transitionProps={{ transition: 'pop' }}>
                      <Group justify="left" gap={2}>
                        <Icon {...iconMap.get(type)!} />
                        <Text>{bloodData.get(type)?.descr}</Text>
                      </Group>
                    </Tooltip>
                  ))}
                </Group>
              </Tooltip.Group>
              <Text size="sm" c="dimmed">
                {t('game.content.scoreboard_note')}
              </Text>
            </Stack>
          </Box>
        </Box>
        <Group justify="space-between">
          <Text size="sm" c="dimmed">
            {t('game.content.scoreboard_tip')}
          </Text>
          <Pagination
            value={activePage}
            onChange={setPage}
            total={Math.ceil((filteredList?.length ?? 1) / ITEM_COUNT_PER_PAGE)}
            boundaries={2}
          />
        </Group>
      </Stack>
      <ScoreboardItemModal
        scoreboard={scoreboard}
        divisionMap={divisionMap}
        bloodBonusMap={bloodData}
        opened={itemDetailOpened}
        withCloseButton={false}
        size="45rem"
        onClose={() => setItemDetailOpened(false)}
        item={currentItem}
      />
    </Paper>
  )
}
