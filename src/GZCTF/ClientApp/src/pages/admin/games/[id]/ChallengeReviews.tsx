import {
    ActionIcon,
    Avatar,
    Box,
    Card,
    Center,
    Grid,
    Group,
    Pagination,
    RingProgress,
    ScrollArea,
    SegmentedControl,
    Skeleton,
    Stack,
    Text,
    TextInput,
    Title,
    Tooltip,
} from '@mantine/core'
import { mdiThumbUp, mdiThumbDown, mdiRefresh, mdiAccount, mdiClockOutline, mdiMagnify, mdiChartBar } from '@mdi/js'
import { Icon } from '@mdi/react'
import cx from 'clsx'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { useDebouncedValue } from '@mantine/hooks'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { ScrollingText } from '@Components/ScrollingText'
import api, { ReviewRating } from '@Api'
import misc from '@Styles/Misc.module.css'

const REVIEW_PER_PAGE = 20

const ChallengeReviews: FC = () => {
    const { id } = useParams()
    const numId = parseInt(id ?? '-1', 10)
    const { t } = useTranslation()
    const [activePage, setPage] = useState(1)
    const [search, setSearch] = useState('')
    const [debouncedSearch] = useDebouncedValue(search, 500)
    const [ratingFilter, setRatingFilter] = useState<string>('all')

    const getRatingFilterValue = () => {
        if (ratingFilter === 'like') return ReviewRating.Like
        if (ratingFilter === 'dislike') return ReviewRating.Dislike
        return undefined
    }

    const { data: reviewResponse, mutate, isLoading } = api.edit.useEditGetReviews(
        numId,
        {
            count: REVIEW_PER_PAGE,
            skip: (activePage - 1) * REVIEW_PER_PAGE,
            search: debouncedSearch,
            rating: getRatingFilterValue()
        },
        { refreshInterval: 0 }
    )

    const { data: analytics } = api.edit.useEditGetReviewAnalytics(numId, { refreshInterval: 0 })

    const reviews = reviewResponse?.data
    const totalCount = reviewResponse?.total ?? 0
    const totalPages = Math.ceil(totalCount / REVIEW_PER_PAGE)

    const cards = isLoading
        ? Array.from({ length: 5 }).map((_, i) => (
            <Card key={i} shadow="sm" radius="md" withBorder p="sm">
                <Grid align="center" gutter="xs">
                    <Grid.Col span={3}>
                        <Stack gap={4}>
                            <Skeleton height={20} width="80%" radius="xl" />
                            <Skeleton height={15} width="40%" radius="xl" />
                        </Stack>
                    </Grid.Col>
                    <Grid.Col span={2}>
                        <Stack gap={4}>
                            <Skeleton height={15} width="90%" radius="xl" />
                            <Skeleton height={15} width="60%" radius="xl" />
                        </Stack>
                    </Grid.Col>
                    <Grid.Col span={7}>
                        <Skeleton height={40} radius="md" />
                    </Grid.Col>
                </Grid>
            </Card>
        ))
        : (Array.isArray(reviews) ? reviews : [])?.map((review) => {
            const borderColor =
                review.rating === ReviewRating.Like
                    ? 'var(--mantine-color-teal-8)'
                    : review.rating === ReviewRating.Dislike
                        ? 'var(--mantine-color-red-8)'
                        : undefined

            return (
                <Card
                    key={review.id}
                    shadow="sm"
                    radius="md"
                    withBorder
                    p="sm"
                    style={{ borderColor }}
                >
                    <Grid align="center" gutter="xs">
                        {/* Challenge Name & Rating */}
                        <Grid.Col span={3}>
                            <Stack gap={4}>
                                <Text fw={700} truncate title={review.challengeName}>
                                    {review.challengeName}
                                </Text>
                                <Group gap={4}>
                                    {review.rating === ReviewRating.Like && (
                                        <Group gap={4} c="teal">
                                            <Icon path={mdiThumbUp} size={0.7} />
                                            <Text size="xs" fw={500}>{t('common.label.like', 'Recommended')}</Text>
                                        </Group>
                                    )}
                                    {review.rating === ReviewRating.Dislike && (
                                        <Group gap={4} c="red">
                                            <Icon path={mdiThumbDown} size={0.7} />
                                            <Text size="xs" fw={500}>{t('common.label.dislike', 'Not Recommended')}</Text>
                                        </Group>
                                    )}
                                </Group>
                            </Stack>
                        </Grid.Col>

                        {/* User Info & Time */}
                        <Grid.Col span={2}>
                            <Stack gap={4}>
                                <Group gap={4} wrap="nowrap" w="100%">
                                    <Icon path={mdiAccount} size={0.7} color="dimmed" />
                                    <Box w="calc(100% - 20px)">
                                        <ScrollingText text={review.userName || ''} size="sm" />
                                    </Box>
                                </Group>
                                <Group gap={4} wrap="nowrap" c="dimmed">
                                    <Icon path={mdiClockOutline} size={0.7} />
                                    <Text size="xs">
                                        {new Date(review.submitTimeUtc ?? 0).toLocaleString()}
                                    </Text>
                                </Group>
                            </Stack>
                        </Grid.Col>

                        {/* Comment */}
                        <Grid.Col span={7}>
                            <ScrollArea.Autosize mah={100} offsetScrollbars>
                                <Text size="sm" style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
                                    {review.comment}
                                </Text>
                            </ScrollArea.Autosize>
                        </Grid.Col>
                    </Grid>
                </Card>
            )
        })

    const analyticsSection = analytics && (
        <Grid mb="md">
            <Grid.Col span={4}>
                <Card withBorder padding="xs" radius="md">
                    <Group>
                        <RingProgress
                            size={70}
                            roundCaps
                            thickness={6}
                            sections={[
                                { value: ((analytics.likes ?? 0) / (analytics.total || 1)) * 100, color: 'teal' },
                                { value: ((analytics.dislikes ?? 0) / (analytics.total || 1)) * 100, color: 'red' },
                            ]}
                            label={
                                <Center>
                                    <Icon path={mdiChartBar} size={1} />
                                </Center>
                            }
                        />
                        <Stack gap={0}>
                            <Text size="sm" fw={700}>
                                {t('admin.analytics.total_reviews', 'Total Reviews')}: {analytics.total}
                            </Text>
                            <Group gap="xs">
                                <Group gap={2} c="teal">
                                    <Icon path={mdiThumbUp} size={0.6} />
                                    <Text size="xs">{analytics.likes ?? 0}</Text>
                                </Group>
                                <Group gap={2} c="red">
                                    <Icon path={mdiThumbDown} size={0.6} />
                                    <Text size="xs">{analytics.dislikes ?? 0}</Text>
                                </Group>
                            </Group>
                        </Stack>
                    </Group>
                </Card>
            </Grid.Col>
            <Grid.Col span={4}>
                <Card withBorder padding="xs" radius="md" h="100%">
                    <Stack gap={2}>
                        <Text size="xs" fw={700} c="teal">
                            {t('admin.analytics.top_liked', 'Top Liked')}
                        </Text>
                        {(analytics.topLiked ?? []).length > 0 ? (
                            (analytics.topLiked ?? []).slice(0, 2).map((c) => (
                                <Group key={c.id} justify="space-between" wrap="nowrap">
                                    <Text size="xs" truncate maw={150}>
                                        {c.title}
                                    </Text>
                                    <Text size="xs" fw={700}>
                                        {c.count}
                                    </Text>
                                </Group>
                            ))
                        ) : (
                            <Text size="xs" c="dimmed">
                                -
                            </Text>
                        )}
                    </Stack>
                </Card>
            </Grid.Col>
            <Grid.Col span={4}>
                <Card withBorder padding="xs" radius="md" h="100%">
                    <Stack gap={2}>
                        <Text size="xs" fw={700} c="red">
                            {t('admin.analytics.top_disliked', 'Top Disliked')}
                        </Text>
                        {(analytics.topDisliked ?? []).length > 0 ? (
                            (analytics.topDisliked ?? []).slice(0, 2).map((c) => (
                                <Group key={c.id} justify="space-between" wrap="nowrap">
                                    <Text size="xs" truncate maw={150}>
                                        {c.title}
                                    </Text>
                                    <Text size="xs" fw={700}>
                                        {c.count}
                                    </Text>
                                </Group>
                            ))
                        ) : (
                            <Text size="xs" c="dimmed">
                                -
                            </Text>
                        )}
                    </Stack>
                </Card>
            </Grid.Col>
        </Grid>
    )

    return (
        <WithGameEditTab
            headProps={{ justify: 'apart' }}
            isLoading={!reviewResponse && !analytics}
            head={
                <Group justify="space-between" wrap="nowrap" w="100%">
                    <Group gap="xs">
                        <Title order={3}>{t('admin.title.challenge_reviews', "Challenge Reviews")}</Title>
                        {totalCount > 0 && <Text size="sm" c="dimmed">({totalCount})</Text>}
                    </Group>
                    <Group gap="xs">
                        <SegmentedControl
                            size="xs"
                            value={ratingFilter}
                            onChange={(val) => {
                                setRatingFilter(val)
                                setPage(1)
                            }}
                            data={[
                                { label: t('common.label.all', 'All'), value: 'all' },
                                { label: t('common.label.like', 'Recommended'), value: 'like' },
                                { label: t('common.label.dislike', 'Not Recommended'), value: 'dislike' },
                            ]}
                        />
                        <TextInput
                            leftSection={<Icon path={mdiMagnify} size={1} />}
                            placeholder={t('common.placeholder.search', 'Search')}
                            value={search}
                            onChange={(e) => {
                                setSearch(e.currentTarget.value)
                                setPage(1)
                            }}
                        />
                        <ActionIcon onClick={() => { mutate(); api.edit.useEditGetReviewAnalytics(numId).mutate() }}>
                            <Icon path={mdiRefresh} size={1} />
                        </ActionIcon>
                    </Group>
                </Group>
            }
        >
            <ScrollArea type="never" pos="relative" h="calc(100vh - 250px)">
                {!reviews && !isLoading ? (
                    <Center h="calc(100vh - 200px)">
                        <Stack gap={0}>
                            <Title order={2}>{t('common.content.no_data', "No Data")}</Title>
                        </Stack>
                    </Center>
                ) : (
                    <Stack gap="md" p="md">
                        {analyticsSection}
                        {cards}
                        {!isLoading && reviews && reviews.length === 0 && (
                            <Center p="xl">
                                <Text c="dimmed">{t('common.content.no_data', 'No Data')}</Text>
                            </Center>
                        )}
                    </Stack>
                )}
            </ScrollArea>
            <Pagination
                value={activePage}
                onChange={setPage}
                total={totalPages > 0 ? totalPages : 1}
                classNames={{
                    root: cx(misc.flex, misc.flexRow, misc.justifyEnd),
                }}
            />
        </WithGameEditTab>
    )
}

export default ChallengeReviews
